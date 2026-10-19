# Exercise 002 — Real dependencies (nuget.resolve)

## Goal

Add an external NuGet package to the build, pinned reproducibly.

## Starting state

The given `src/TicTacToe.Console/Program.cs` has been replaced with a version
that renders the game with [Spectre.Console](https://spectreconsole.net/).
Right now the build is **broken** — try it:

```sh
bazel run //:app
```

You get `error CS0246: The type or namespace name 'Spectre' could not be
found`. Bazel has no idea where `Spectre.Console` comes from. Fixing that is
your job.

## Steps

1. Add the `nuget` module extension to `MODULE.bazel`:
   - `use_extension("@rules_dotnet//dotnet:nuget.bzl", "nuget")`;
   - a `resolve` tag with `name = "nuget"`,
     `packages = {"Spectre.Console": "0.49.1"}`,
     `frameworks = ["net10.0"]`, and `lock_file = "//:nuget_resolve.json"`;
   - `use_repo(nuget, "nuget.net10.0")`.

   Each target framework gets its own hub repository; a package in `net10.0`
   is addressed as `@nuget.net10.0//<package-id-lowercase>`.

2. Create the pin. The lock file must exist (valid, but empty of packages)
   before the extension will evaluate — create a seed `nuget_resolve.json`
   (exact content in the hint), then run:

   ```sh
   REPIN=1 bazel run @nuget.net10.0//:pin
   ```

   It prints `Updated nuget_resolve.json.` Inspect the file: versions, sha512
   hashes, and download URLs are pinned. Commit the lock file.

3. Work out the Bazel label of the pinned package. The rule: *lowercased
   package id in the per-framework repository*. Apply it to Spectre.Console
   and add the result to `deps` of the `app` target in `BUILD.bazel`.

4. Run the game with its new UI (use a real terminal, not an IDE panel):

   ```sh
   bazel run //:app
   ```

## Expected outcome

A playable game rendered with Spectre.Console (colors, selection prompt), and
a checked-in `nuget_resolve.json`.

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

nuget = use_extension("@rules_dotnet//dotnet:nuget.bzl", "nuget")
nuget.resolve(
    name = "nuget",
    packages = {
        "Spectre.Console": "0.49.1",
    },
    frameworks = ["net10.0"],
    lock_file = "//:nuget_resolve.json",
)
use_repo(nuget, "nuget.net10.0")
```

</details>

<details>
<summary>Hint: seed nuget_resolve.json and the pin command</summary>

Seed content (the pin rewrites it with the resolved packages):

```json
{
  "version": 1,
  "inputHash": "",
  "frameworks": {
    "net10.0": {
      "packages": []
    }
  }
}
```

Then:

```sh
REPIN=1 bazel run @nuget.net10.0//:pin
git add nuget_resolve.json
git commit -m "Pin Spectre.Console"
```

</details>

<details>
<summary>Hint: complete BUILD.bazel</summary>

The label rule applied to `Spectre.Console`: repository `nuget.net10.0`,
package `spectre.console`.

```starlark
load("@rules_dotnet//dotnet:defs.bzl", "csharp_binary")

csharp_binary(
    name = "app",
    srcs = glob([
        "src/TicTacToe.Console/*.cs",
        "src/TicTacToe.Engine/*.cs",
    ]),
    target_frameworks = ["net10.0"],
    deps = ["@nuget.net10.0//spectre.console"],
)
```

</details>

## Questions

1. *What happens if you delete `nuget_resolve.json` and build?*
   <details><summary>Answer</summary>
   The `lock_file` label no longer exists, so the build fails while the module
   extension evaluates. Without a `lock_file` attribute at all, every fetch
   would resolve fresh and builds would not be reproducible.
   </details>
2. *Why is the repository called `nuget.net10.0`?*
   <details><summary>Answer</summary>
   `nuget.resolve` creates one hub repository per target framework. With
   several frameworks you would depend on e.g. both `@nuget.net8.0//...` and
   `@nuget.net10.0//...`.
   </details>
3. *What does `REPIN=1` actually change?*
   <details><summary>Answer</summary>
   It tells the `:pin` target to rewrite the lock file from a fresh resolution
   instead of validating the build against the existing lock file.
   </details>

## Next

Run `./next_exercise.sh` to move on to exercise 003.
