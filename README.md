# Exercise 001 — Your first Bazel module

## Goal

Write a minimal bzlmod module that builds and runs the tic-tac-toe game.

## Steps

1. Create `MODULE.bazel` in the repository root. It needs three things:
   - a `module(...)` declaration with `name = "dotnet_workshop"`;
   - a `bazel_dep` on `rules_dotnet` (any version string works, e.g.
     `"0.0.0"` — the next item overrides where it comes from);
   - a `git_override` for `rules_dotnet` pointing at
     `https://github.com/juulhobert/rules_dotnet.git`, pinned to a specific
     commit.

   *Why the override?* rules_dotnet (with the Gazelle C# extension and
   `nuget.resolve`) is not yet published to the Bazel Central Registry, so
   the workshop fetches it straight from git. The override is pinned to a
   commit (not a moving branch) so the workshop is reproducible. The exact
   commit is in the hint below.

2. Create `BUILD.bazel` in the repository root with a single handwritten
   `csharp_binary` named `app`:
   - load `csharp_binary` from `@rules_dotnet//dotnet:defs.bzl`;
   - `srcs` globs both `src/TicTacToe.Console/*.cs` and
     `src/TicTacToe.Engine/*.cs`;
   - `target_frameworks = ["net10.0"]`.

3. Run the game:

   ```sh
   bazel run //:app
   ```

   The first build takes a while: Bazel downloads the .NET SDK by itself.
   Play a full game — you are X, the computer is O.

## Expected outcome

A playable game of tic-tac-toe in your terminal, built entirely by Bazel.

<details>
<summary>Hint: complete MODULE.bazel</summary>

```starlark
module(
    name = "dotnet_workshop",
    version = "0.0.0",
)

bazel_dep(name = "rules_dotnet", version = "0.0.0")
git_override(
    module_name = "rules_dotnet",
    commit = "93e61b795e0c1e7b8bb4fc4fbc30b558cbbb3aa8",
    remote = "https://github.com/juulhobert/rules_dotnet.git",
)
```

</details>

<details>
<summary>Hint: complete BUILD.bazel</summary>

```starlark
load("@rules_dotnet//dotnet:defs.bzl", "csharp_binary")

csharp_binary(
    name = "app",
    srcs = glob([
        "src/TicTacToe.Console/*.cs",
        "src/TicTacToe.Engine/*.cs",
    ]),
    target_frameworks = ["net10.0"],
)
```

</details>

## Questions

1. *Why does one binary target compiling both directories work here, and when
   would that stop being a good idea?*
   <details><summary>Answer</summary>
   For a toy program no dependency boundary is needed — one compilation is
   simplest. Real projects split libraries from binaries for reuse, build
   caching, and independent testing. Exercise 003 introduces that split
   automatically.
   </details>
2. *Where did the .NET SDK come from? You never installed it.*
   <details><summary>Answer</summary>
   rules_dotnet registers and downloads a .NET toolchain as part of the build;
   bazelisk/Bazel fetched Bazel itself. Nothing .NET-specific had to be
   installed on your machine.
   </details>

## Next

Run `./next_exercise.sh` to move on to exercise 002.
