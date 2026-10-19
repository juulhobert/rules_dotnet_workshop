# rules_dotnet workshop: Gazelle + NuGet

A hands-on codelab that teaches you how to build C# with Bazel using
[rules_dotnet](https://github.com/juulhobert/rules_dotnet): declaring a module,
resolving and pinning NuGet packages with `nuget.resolve`, and generating +
maintaining BUILD files with the Gazelle C# extension.

You will build a console tic-tac-toe game played against the computer. **All
C# code is given.** You write only the Bazel parts: `MODULE.bazel`,
`BUILD.bazel`, the NuGet pin, and the Gazelle setup.

## What you will learn

- A minimal bzlmod module: `bazel_dep`, and `git_override` (rules_dotnet is
  not yet published to the Bazel Central Registry).
- `csharp_binary` / `csharp_library` from rules_dotnet.
- `nuget.resolve`: package resolution, the `nuget_resolve.json` lock file,
  and repinning with `REPIN=1 bazel run @nuget.net10.0//:pin`.
- Gazelle: generating per-directory BUILD files from your C# sources and the
  lock file, and iterating (add package → repin → rerun Gazelle).

## Prerequisites

You need exactly:

- **bazelisk** available as `bazel` on your `PATH`. Bazel downloads the .NET
  SDK and the Go toolchain itself; no local .NET or Go install is required.
- **git**.
- **A POSIX shell** to run `next_exercise.sh` (on Windows: Git Bash or WSL).
- **Internet access** (Bazel fetches rules_dotnet, toolchains, and packages).
- **A real terminal** (not an IDE output panel): from exercise 002 onward the
  game UI uses Spectre.Console, which needs an interactive terminal.

## How the workshop works

There is one branch per exercise:

```
main → exercise-000-intro → exercise-001-hello-bazel
     → exercise-002-nuget-resolve → exercise-003-gazelle → exercise-004-change
```

Each branch contains everything you need to start that exercise, including a
`README.md` with the steps, hints (full reference solutions hidden in
collapsible blocks), and reflection questions.

To move to the next exercise, run:

```sh
./next_exercise.sh
```

Then open `README.md` again.

## WARNING: next_exercise.sh is destructive

`next_exercise.sh` runs `git clean -fdx` and `git reset --hard` before
switching branches. **All uncommitted changes and untracked files are
permanently lost.** The script warns you and makes you type `yes` first.

Committed work survives (`git reset --hard` resets to your HEAD, keeping your
local commits). If you experiment, either commit your experiments locally or
use `git stash` before hopping branches.

## Start

```sh
./next_exercise.sh
```
