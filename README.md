# Exercise 003 — Stop writing BUILD files (Gazelle)

## Goal

Generate and maintain the BUILD files with Gazelle instead of writing them by
hand.

## Starting state

The working exercise-002 solution: a handwritten `csharp_binary` in the root
`BUILD.bazel`, Spectre.Console pinned in `nuget_resolve.json`.

## Steps

1. Gazelle is a Go program, so add two `bazel_dep`s to `MODULE.bazel`:
   `bazel_gazelle` and `rules_go` (exact versions in the hint — they must be
   compatible with the pinned rules_dotnet commit). Note: in the Bazel Central
   Registry the module is called `gazelle`; the dep uses
   `repo_name = "bazel_gazelle"` so it keeps its traditional repository name.

2. Rewrite the root `BUILD.bazel`:
   - `load("@bazel_gazelle//:def.bzl", "gazelle", "gazelle_binary")`;
   - `package(default_visibility = ["//visibility:public"])`;
   - the directive comment `# gazelle:dotnet_target_frameworks net10.0`;
   - a `gazelle_binary` named `gazelle_bin` with
     `languages = ["@rules_dotnet//dotnet/gazelle"]`;
   - a `gazelle` target named `gazelle` with
     `extra_args = ["-dotnet_nuget_lock_file=nuget_resolve.json", "."]` and
     `gazelle = "gazelle_bin"`.

   The walk directory is `.` (the repository root), so the root directive
   applies to everything below it.

3. Delete the handwritten `csharp_binary` (it is replaced by the generated
   targets — and its `srcs` glob would break the moment the generated BUILD
   files turn `src/TicTacToe.*` into subpackages). Then run:

   ```sh
   bazel run //:gazelle
   ```

4. Study what Gazelle generated:
   - `src/TicTacToe.Engine/BUILD.bazel` — a `csharp_library` named
     `tictactoe_engine`;
   - `src/TicTacToe.Console/BUILD.bazel` — a `csharp_binary` named `program`
     whose `deps` contain both `//src/TicTacToe.Engine:tictactoe_engine` and
     `@nuget.net10.0//spectre.console`, resolved automatically from your
     `using` directives and `nuget_resolve.json`.

5. Keep the familiar verification command working by adding an alias to the
   root `BUILD.bazel`:

   ```starlark
   alias(
       name = "app",
       actual = "//src/TicTacToe.Console:program",
   )
   ```

6. Run the game:

   ```sh
   bazel run //:app
   ```

## Expected outcome

The same playable game, but the BUILD files in `src/` are now generated — and
can be regenerated any time with `bazel run //:gazelle`.

<details>
<summary>Hint: the MODULE.bazel additions</summary>

```starlark
bazel_dep(name = "gazelle", version = "0.51.3", repo_name = "bazel_gazelle")
bazel_dep(name = "rules_go", version = "0.59.0")
```

</details>

<details>
<summary>Hint: complete root BUILD.bazel</summary>

```starlark
load("@bazel_gazelle//:def.bzl", "gazelle", "gazelle_binary")

package(default_visibility = ["//visibility:public"])

# gazelle:dotnet_target_frameworks net10.0

gazelle_binary(
    name = "gazelle_bin",
    languages = ["@rules_dotnet//dotnet/gazelle"],
)

gazelle(
    name = "gazelle",
    extra_args = [
        "-dotnet_nuget_lock_file=nuget_resolve.json",
        ".",
    ],
    gazelle = "gazelle_bin",
)

alias(
    name = "app",
    actual = "//src/TicTacToe.Console:program",
)
```

</details>

<details>
<summary>Hint: the two generated files</summary>

`src/TicTacToe.Engine/BUILD.bazel`:

```starlark
load("@rules_dotnet//dotnet:defs.bzl", "csharp_library")

csharp_library(
    name = "tictactoe_engine",
    srcs = [
        "Board.cs",
        "ComputerPlayer.cs",
        "Player.cs",
    ],
    project_sdk = "default",
    target_frameworks = ["net10.0"],
    visibility = ["//visibility:public"],
)
```

`src/TicTacToe.Console/BUILD.bazel`:

```starlark
load("@rules_dotnet//dotnet:defs.bzl", "csharp_binary")

csharp_binary(
    name = "program",
    srcs = ["Program.cs"],
    project_sdk = "default",
    target_frameworks = ["net10.0"],
    visibility = ["//visibility:public"],
    deps = [
        "//src/TicTacToe.Engine:tictactoe_engine",
        "@nuget.net10.0//spectre.console",
    ],
)
```

</details>

## Questions

1. *How did Gazelle know that `using Spectre.Console;` means
   `@nuget.net10.0//spectre.console`?*
   <details><summary>Answer</summary>
   The `-dotnet_nuget_lock_file` flag points Gazelle at `nuget_resolve.json`;
   C# namespaces map to package ids from the lock file via longest-prefix
   match.
   </details>
2. *How did Gazelle know about the Engine dependency?*
   <details><summary>Answer</summary>
   Every generated library indexes the namespaces declared by its sources; a
   `using` in another directory resolves through Gazelle's resolve index to
   that library's label.
   </details>
3. *Why did the target names come out as `tictactoe_engine` and `program`?*
   <details><summary>Answer</summary>
   A library is named after its directory, sanitized to a valid Bazel target
   name (`TicTacToe.Engine` → `tictactoe_engine`: lowercased, dot removed); a
   binary is named after its entry-point file stem (`Program.cs` → `program`).
   </details>

## Next

Run `./next_exercise.sh` to move on to exercise 004.
