# Design: Gazelle + NuGet hands-on workshop (codelab) for rules_dotnet

Date: 2026-10-19
Status: approved by user (brainstorm complete)

## Goal

A standalone, hands-on workshop (codelab) that teaches participants how to use
the new Gazelle C# extension and the `nuget.resolve` module extension from
rules_dotnet. Participants build a console tic-tac-toe game played against the
computer. All C# code is given; participants write only the Bazel parts
(`MODULE.bazel`, `BUILD.bazel`, NuGet pinning, Gazelle setup). The format is
modeled on [EngFlow-Academy/codelabs](https://github.com/EngFlow-Academy/codelabs):
a git repository with one branch per exercise, a destructive `next_exercise.sh`
script, and per-exercise READMEs with numbered steps, collapsible `<details>`
hints, and closing reflection questions.

The workshop lives in its own git repository at
`/home/jhobert/Projects/Gazelle/dotnet_workshop`. All workshop content
(READMEs, hints, questions, code comments) is in English.

### Audience

Developers who know basic C# and have heard of Bazel but have never (or barely)
used rules_dotnet, `nuget.resolve`, or Gazelle for C#. The workshop assumes no
prior Bazel-module-authoring experience; every Bazel concept used is introduced
in the exercise where it first appears.

## Non-Goals

The workshop explicitly does **not** cover:

- **Unit tests.** No `csharp_nunit_test`/xunit/MSTest targets, no test
  framework packages. Keeping the dependency surface minimal keeps the focus on
  resolution and generation.
- **Multiple target frameworks.** Everything targets `net10.0` only. The
  multi-framework capability of `nuget.resolve` (one hub repository per TFM) is
  mentioned in a reflection answer but never exercised.
- **F#, ASP.NET, Blazor, Razor, containers, or publishing** (`dotnet publish`,
  self-contained deployments, container images).
- **WORKSPACE mode.** bzlmod only.
- **Advanced Gazelle configuration.** Only the
  `# gazelle:dotnet_target_frameworks` directive and the
  `-dotnet_nuget_lock_file` flag are used. `dotnet_resolve` overrides,
  merge/`# keep` semantics, and warn/fail modes for unknown imports are
  mentioned at most in passing in a reflection answer.
- **Private NuGet feeds, netrc credentials, or version ranges.** All packages
  come from nuget.org with exact versions.
- **Teaching C# itself.** The C# code is given and participants do not modify it.
- **CI setup for participants.** (CI for the workshop repository itself is in
  scope; see Testing Strategy.)

## Prerequisites

Participants need exactly:

- **bazelisk** (as `bazel` on the `PATH`). Bazel fetches the .NET SDK and the
  Go toolchain itself; no local .NET or Go installation is required.
- **git** (to clone the workshop repository and for `next_exercise.sh`).
- **A POSIX shell** to run `next_exercise.sh`. On Windows this means Git Bash
  (bundled with git for Windows) or WSL.
- **Internet access** (Bazel fetches rules_dotnet, the .NET SDK, the Go
  toolchain, and NuGet packages).
- **A real terminal** (not an IDE output panel) for `bazel run //:app`, because
  the Spectre.Console UI needs an interactive terminal.

## Context

### Why `git_override`

`rules_dotnet` (with the Gazelle C# extension and `nuget.resolve`) is not yet
published to the Bazel Central Registry. The workshop therefore consumes it via
a `git_override` pointing at
`https://github.com/juulhobert/rules_dotnet.git`. The override is pinned to a
specific **commit** on the `feature/gazelle` branch (not the moving branch
head), so the workshop is reproducible; the pinned commit is recorded in the
repository's `MODULE.bazel` and updated deliberately when rules_dotnet
advances (see Risks and Mitigations).

### `nuget.resolve` usage taught by the workshop

```starlark
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

Each target framework gets its own hub repository; a package in `net10.0` is
addressed as `@nuget.net10.0//<package-id-lowercase>` (e.g.
`@nuget.net10.0//spectre.console`). Without `lock_file` every fetch resolves
fresh. The lock file is created/refreshed with
`REPIN=1 bazel run @nuget.net10.0//:pin` and checked into git.

### Gazelle usage taught by the workshop

Modeled on rules_dotnet's own `examples/gazelle_csharp`:

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
```

Using Gazelle additionally requires `bazel_dep`s on `bazel_gazelle` and
`rules_go` from the registry. Their exact versions are chosen at
implementation time (the newest pair compatible with the pinned rules_dotnet
commit) and pinned identically in the exercise-003 hint and reference
solution, so participants and CI always use the same versions. The walk
directory is `.` (the repository root) so that the
`# gazelle:dotnet_target_frameworks` directive in the root `BUILD.bazel`
applies to everything below; directories without `.cs` files (such as `docs/`)
produce no output.

Relevant generated-target naming rules (from
`rules_dotnet/dotnet/gazelle/generate.go`): a `csharp_library` is named after
its directory, lowercased with characters outside `[a-z0-9_-]` replaced by `_`;
a `csharp_binary` is named after its entry-point file stem, sanitized the same
way. For this workshop that yields:

- `//src/TicTacToe.Engine:tictactoe_engine` (`csharp_library`)
- `//src/TicTacToe.Console:program` (`csharp_binary`)

## Proposed Architecture

### Repository layout

Standalone git repository `dotnet_workshop/` (to be initialized as a git repo
when implementation starts; this spec only covers content):

```
dotnet_workshop/
├── MODULE.bazel          # final-state: rules_dotnet via git_override + nuget.resolve + gazelle deps
├── BUILD.bazel           # final-state: alias //:app + gazelle targets
├── nuget_resolve.json    # final-state: pinned Spectre.Console + Newtonsoft.Json
├── next_exercise.sh      # destructive branch hopper (see below)
├── README.md             # landing page: intro + prerequisites
├── docs/                 # workshop maintenance docs (incl. this spec)
└── src/
    ├── TicTacToe.Engine/
    │   ├── Board.cs
    │   ├── Player.cs
    │   └── ComputerPlayer.cs
    └── TicTacToe.Console/
        └── Program.cs
```

The tree above shows the final (exercise-004 solution) state. Individual
branches contain only the files appropriate to their stage (see below).

### Branch layout

Each exercise branch contains the **complete starting state** for that
exercise: the reference solution of all previous exercises plus any new given
files. `main` is the landing page.

| Branch | Contents | Participant task |
|---|---|---|
| `main` | `README.md` (intro, prerequisites, how to start), `next_exercise.sh` | Read, then `./next_exercise.sh` |
| `exercise-000-intro` | `src/` (plain-console version of the C# code), `README.md`, `next_exercise.sh`. **No Bazel files at all.** | Explore; observe that `bazel run //:app` fails |
| `exercise-001-hello-bazel` | Exercise-000 content, with the README replaced by exercise 001's steps | Write `MODULE.bazel` (bazel_dep + git_override) and a handwritten `csharp_binary`; `bazel run //:app` |
| `exercise-002-nuget-resolve` | Exercise-001 **solution** + Spectre.Console-based `Program.cs` replacing the plain one | Add `nuget.resolve`, pin Spectre.Console, check in `nuget_resolve.json`, add the dep manually |
| `exercise-003-gazelle` | Exercise-002 **solution** | Add `gazelle_binary` + `gazelle` targets, run Gazelle, replace handwritten target with an alias |
| `exercise-004-change` | Exercise-003 **solution** + given `HighScores.cs` + updated given `Program.cs` that calls it | Add Newtonsoft.Json to `packages`, repin, rerun Gazelle, observe automatic dep resolution |

Branch names follow the EngFlow `exercise-NNN-name` convention.

### `next_exercise.sh` behavior

A bash script at the repository root, present (identical) on every branch:

1. Prints a prominent warning that it is **destructive**: all uncommitted
   changes and untracked files in the working tree will be permanently lost,
   and the participant must type `yes` to confirm.
2. Determines the current branch and looks up the next branch in the fixed
   sequence:
   `main` → `exercise-000-intro` → `exercise-001-hello-bazel` →
   `exercise-002-nuget-resolve` → `exercise-003-gazelle` →
   `exercise-004-change`.
3. Runs `git clean -fdx` and `git reset --hard` on the current branch, then
   `git checkout <next-branch>`.
4. Prints the name of the new exercise and instructs the participant to open
   its `README.md`.
5. On `exercise-004-change` (the last exercise) it prints a
   "workshop complete" message instead of checking out anything.

### The given C# application

Participants never modify C# code, but the given code differs per branch (see
below). Two components, deliberately small:

**`src/TicTacToe.Engine/`** — game logic, no external dependencies:

- `Player.cs` — `enum Player { X, O }`.
- `Board.cs` — 3x3 grid; place a mark; detect win (rows, columns, diagonals)
  and draw; expose cell state for rendering.
- `ComputerPlayer.cs` — picks a move with a simple priority: winning move >
  blocking move > random empty cell.

**`src/TicTacToe.Console/`** — the UI:

- `Program.cs` — game loop: render the board, prompt the human for a cell, let
  the computer move, repeat until win/draw.
  - In `exercise-000-intro` and `exercise-001-hello-bazel`: a **plain**
    version using only `System.Console` (no external dependencies), so the
    first Bazel build needs nothing but rules_dotnet.
  - From `exercise-002-nuget-resolve` onward: a **Spectre.Console** version
    rendering the board with `Table`/`Grid`, a `SelectionPrompt` for picking a
    cell, and per-player colors. This is a *given* replacement on the
    exercise-002 branch, not a participant edit.
- `HighScores.cs` — added (given) on `exercise-004-change` only; persists
  results to a JSON file using **Newtonsoft.Json**. The exercise-004 branch
  also ships an updated given `Program.cs` that calls into it.

Rationale for the two versions of `Program.cs`: exercise 001 must produce a
runnable game before NuGet resolution has been introduced, and exercise 002
must start from a *broken* build (the given code now references Spectre) so
that adding `nuget.resolve` is the motivated fix — mirroring real projects.
This gives exactly the two dependency kinds the workshop teaches: an
**internal library dependency** (Gazelle maps `using TicTacToe.Engine;` to
`//src/TicTacToe.Engine:tictactoe_engine`) and an **external NuGet
dependency** (namespace → package via `nuget_resolve.json`).

## Per-exercise detailed content

Every exercise README follows the same structure:

1. **Goal** — one short section.
2. **Numbered steps** — explicit commands the participant must construct
   themselves; not everything is copy-paste. Where a command must be derived
   (e.g. "figure out the label of the pinned package"), the step states the
   *shape* of the answer.
3. **Hints** — `<details>`-collapsible blocks containing the **complete
   reference solution** for that exercise (full file contents/diffs and
   commands), so a stuck participant is never blocked.
4. **Questions** — 2–3 reflection questions, answers hidden in `<details>`.
5. **Next** — instruction to run `./next_exercise.sh`.

Verification after every exercise (except 000) is a visible result:
`bazel run //:app` plays tic-tac-toe. No abstract checks.

### exercise-000-intro — "It doesn't build yet"

- **Goal:** understand what Bazel needs before it can do anything.
- **Steps:**
  1. Inspect `src/`; identify the two components and their relationship.
  2. Run `bazel run //:app` and read the error (no `MODULE.bazel`, no `BUILD`
     files).
  3. Run `bazel query //...` and observe it fails too.
- **Expected outcome:** clear failure — this is intentional and is the
  starting point of the learning process.
- **Hint content:** explanation of the error messages and a pointer to what
  exercise 001 will add.
- **Questions:**
  1. *Why does Bazel refuse to build before any BUILD file exists?* (Answer:
     Bazel builds targets, not source trees; a target must be declared.)
  2. *What two files does Bazel need at minimum in a bzlmod project?*
     (Answer: `MODULE.bazel` and a `BUILD.bazel` with a target.)

### exercise-001-hello-bazel — "Your first Bazel module"

- **Goal:** write a minimal bzlmod module that builds and runs the game.
- **Steps:**
  1. Create `MODULE.bazel`: `module(name = "dotnet_workshop", ...)`, a
     `bazel_dep` on `rules_dotnet`, and a `git_override` to
     `https://github.com/juulhobert/rules_dotnet.git` (the README explains
     *why* the override is needed: rules_dotnet is not in the registry yet;
     the pinned commit is given in the hint).
  2. Create a root `BUILD.bazel` with a single handwritten `csharp_binary`
     named `app` whose `srcs` glob both `src/TicTacToe.Engine/*.cs` and
     `src/TicTacToe.Console/*.cs`, with `target_frameworks = ["net10.0"]`.
  3. `bazel run //:app` — play a full game in the plain console UI.
- **Expected outcome:** playable tic-tac-toe; participant has seen that Bazel
  downloaded the .NET SDK by itself.
- **Hint content:** complete `MODULE.bazel` and `BUILD.bazel`.
- **Questions:**
  1. *Why does one binary target compiling both directories work here, and
     when would that stop being a good idea?* (Answer: no dependency boundary
     is needed for a toy; real projects want a library/binary split for
     reuse, caching, and tests — exercise 003 does this automatically.)
  2. *Where did the .NET SDK come from? You never installed it.* (Answer:
     rules_dotnet registers and downloads a toolchain; bazelisk/Bazel fetch
     everything.)

### exercise-002-nuget-resolve — "Real dependencies"

- **Goal:** add an external NuGet package with reproducible pinning.
- **Starting state:** the given `Program.cs` now uses Spectre.Console, so
  `bazel run //:app` fails with missing-type errors. The failure is the
  motivation.
- **Steps:**
  1. Add the `nuget` module extension to `MODULE.bazel` with a `resolve` tag:
     `name = "nuget"`, `packages = {"Spectre.Console": "0.49.1"}`,
     `frameworks = ["net10.0"]`, `lock_file = "//:nuget_resolve.json"`, plus
     `use_repo(nuget, "nuget.net10.0")`.
  2. Create the pin: `REPIN=1 bazel run @nuget.net10.0//:pin`, then inspect
     the generated `nuget_resolve.json` (versions, sha512, URLs) and commit
     it.
  3. Work out the Bazel label of the package
     (`@nuget.net10.0//spectre.console` — the README states the rule
     "lowercased package id in the per-framework repository", the participant
     applies it) and add it to `deps` of the handwritten `app` target.
  4. `bazel run //:app` — play the game with the Spectre UI.
- **Expected outcome:** playable game with rich UI; checked-in lock file.
- **Hint content:** the full `MODULE.bazel` diff, the exact pin command, and
  the `deps` line.
- **Questions:**
  1. *What happens if you delete `nuget_resolve.json` and build?* (Answer: the
     `lock_file` label is missing → the build fails; without the `lock_file`
     attribute, every fetch would resolve fresh and builds would not be
     reproducible.)
  2. *Why is the repository called `nuget.net10.0`?* (Answer: one hub
     repository per target framework; with several frameworks you would
     depend on e.g. both `@nuget.net8.0//...` and `@nuget.net10.0//...`.)
  3. *What does `REPIN=1` actually change?* (Answer: it tells the `:pin`
     target to rewrite the lock file from a fresh resolution instead of
     validating against it.)

### exercise-003-gazelle — "Stop writing BUILD files"

- **Goal:** generate and maintain BUILD files with Gazelle.
- **Steps:**
  1. Add `bazel_dep`s on `bazel_gazelle` and `rules_go` (versions given in
     the hint).
  2. Extend the root `BUILD.bazel`: `package(default_visibility = ...)`, the
     `# gazelle:dotnet_target_frameworks net10.0` directive, a
     `gazelle_binary` with `languages = ["@rules_dotnet//dotnet/gazelle"]`,
     and a `gazelle` target with
     `extra_args = ["-dotnet_nuget_lock_file=nuget_resolve.json", "."]`.
  3. Delete the handwritten `csharp_binary` and run `bazel run //:gazelle`.
  4. Study the generated `src/TicTacToe.Engine/BUILD.bazel`
     (`csharp_library` `tictactoe_engine`) and
     `src/TicTacToe.Console/BUILD.bazel` (`csharp_binary` `program` with
     `deps` on both the Engine library and
     `@nuget.net10.0//spectre.console` — resolved automatically from
     `nuget_resolve.json`).
  5. Add `alias(name = "app", actual = "//src/TicTacToe.Console:program")` to
     the root `BUILD.bazel` so the familiar verification command keeps
     working.
  6. `bazel run //:app` — same game, generated BUILD files.
- **Expected outcome:** playable game; participant can explain where each
  generated `deps` entry came from.
- **Hint content:** the full root `BUILD.bazel`, the two generated files, and
  the alias.
- **Questions:**
  1. *How did Gazelle know that `using Spectre.Console;` means
     `@nuget.net10.0//spectre.console`?* (Answer: `-dotnet_nuget_lock_file`
     points at the lock file; namespaces map to package ids via longest-prefix
     match.)
  2. *How did Gazelle know about the Engine dependency?* (Answer: every
     generated library indexes the namespaces of its sources; a `using` in
     another directory resolves via Gazelle's resolve index.)
  3. *Why did the target names come out as `tictactoe_engine` and
     `program`?* (Answer: library = sanitized directory name; binary =
     sanitized entry-point file stem.)

### exercise-004-change — "Iterate like it's a real project"

- **Goal:** experience the full add-a-dependency iteration loop.
- **Starting state:** the branch adds the given `HighScores.cs` (uses
  Newtonsoft.Json) and an updated given `Program.cs` that records results, so
  the build fails: Newtonsoft.Json is neither pinned nor depended upon.
- **Steps:**
  1. Run `bazel run //:app` (or `bazel build //...`) and read the failure.
  2. Add `"Newtonsoft.Json": "13.0.3"` to the `packages` dict in
     `MODULE.bazel`.
  3. Repin: `REPIN=1 bazel run @nuget.net10.0//:pin`; inspect the diff of
     `nuget_resolve.json`.
  4. Rerun `bazel run //:gazelle` and inspect the diff of
     `src/TicTacToe.Console/BUILD.bazel` — the
     `@nuget.net10.0//newtonsoft.json` dep appeared automatically.
  5. `bazel run //:app` — play a game and see the high-score table.
- **Expected outcome:** playable game with high scores; participant has run
  the complete change loop (edit MODULE → repin → regazelle → run).
- **Hint content:** the one-line `packages` change and the exact commands.
- **Questions:**
  1. *Why did you not have to touch any BUILD file in this exercise?*
     (Answer: Gazelle regenerates deps from the source `using` directives and
     the lock file; BUILD files are derived artifacts.)
  2. *What would happen if you repinned but forgot to rerun Gazelle?*
     (Answer: the build still fails — the package is fetchable but no target
     depends on it; resolution and generation are separate steps.)
  3. *Which files in this repository are hand-written and which are generated
     or pinned?* (Answer: hand-written: C# sources, `MODULE.bazel`, root
     `BUILD.bazel` scaffolding; generated: per-directory BUILD files;
     generated-but-checked-in: `nuget_resolve.json`.)

## Files To Change

All files are new; the `dotnet_workshop` repository does not exist yet.

**On `main`:**

- `README.md` — landing page: what the workshop teaches, prerequisites, how to
  start (`./next_exercise.sh`), the destructiveness warning.
- `next_exercise.sh` — as specified above (present identically on all
  branches).

**On `exercise-000-intro` (additionally):**

- `src/TicTacToe.Engine/{Board.cs,Player.cs,ComputerPlayer.cs}` — given.
- `src/TicTacToe.Console/Program.cs` — given, plain `System.Console` version.
- `README.md` — exercise 000 as specified above.

**On `exercise-001-hello-bazel` (additionally):**

- `README.md` — exercise 001 (no solution files; the participant writes
  `MODULE.bazel` and `BUILD.bazel`; the reference solution lives in the hints
  and becomes the starting state of exercise-002).

**On `exercise-002-nuget-resolve` (additionally):**

- `MODULE.bazel` — exercise-001 reference solution (module + `bazel_dep` +
  pinned `git_override`).
- `BUILD.bazel` — exercise-001 reference solution (handwritten `csharp_binary`
  `app`).
- `src/TicTacToe.Console/Program.cs` — given Spectre.Console version
  (replaces the plain one).
- `README.md` — exercise 002.

**On `exercise-003-gazelle` (additionally):**

- `MODULE.bazel` — exercise-002 reference solution (adds `nuget.resolve` +
  `use_repo`).
- `nuget_resolve.json` — exercise-002 reference solution (Spectre.Console
  pinned).
- `BUILD.bazel` — exercise-002 reference solution (handwritten `app` with the
  `spectre.console` dep).
- `README.md` — exercise 003.

**On `exercise-004-change` (additionally):**

- `MODULE.bazel` — exercise-003 reference solution (adds `bazel_gazelle` +
  `rules_go`).
- `BUILD.bazel` — exercise-003 reference solution (directive, `gazelle_bin`,
  `gazelle`, `alias` `app`).
- `src/TicTacToe.Engine/BUILD.bazel`,
  `src/TicTacToe.Console/BUILD.bazel` — exercise-003 reference solution
  (Gazelle-generated, checked in).
- `src/TicTacToe.Console/HighScores.cs` — given, new.
- `src/TicTacToe.Console/Program.cs` — given, updated to use `HighScores`.
- `README.md` — exercise 004.

**Workshop maintenance (`main` only):** `next_exercise.sh` hard-resets every
exercise branch, so maintenance files live on `main` where participants never
need them:

- `docs/superpowers/specs/2026-10-19-dotnet-workshop-design.md` — this spec.
- CI workflow (see Testing Strategy) and the reference-solution patches per
  exercise, stored under `docs/solutions/` on `main` so CI can apply them.

## Testing Strategy

The workshop itself is software and is verified in CI on the workshop
repository:

1. **Per-exercise reference-solution check.** For each exercise branch, CI
   checks out the branch, applies the reference solution for that exercise
   (the exact content of the README `<details>` hints, stored as patches under
   `docs/solutions/`), and runs `bazel build //...`. This guarantees the hints
   are correct and complete — a participant following them can never be
   blocked.
2. **Broken-start check.** On `exercise-000-intro`, `exercise-002-nuget-resolve`
   and `exercise-004-change`, CI asserts that `bazel build //...` **fails** in
   the starting state (the didactic motivation), and passes after the solution
   is applied.
3. **Gazelle idempotence check.** On the exercise-003 and exercise-004
   solutions, CI runs `bazel run //:gazelle` and asserts `git diff --exit-code`
   is clean — generated files match what is checked in.
4. **Lock-file freshness check.** On the final state, CI runs
   `REPIN=1 bazel run @nuget.net10.0//:pin` and asserts `git diff --exit-code`
   is clean.
5. **Smoke run.** On exercise-001 (plain `System.Console` UI), CI pipes a
   scripted game into `bazel run //:app` and asserts a win/draw banner appears
   in the output. From exercise-002 onward the Spectre.Console
   `SelectionPrompt` requires an interactive terminal and cannot be driven via
   redirected stdin, so CI verifies those branches with `bazel build //...`
   plus checks 1–4 above; the interactive run is verified manually by the
   workshop author whenever the pinned rules_dotnet commit or package versions
   change.
6. **`next_exercise.sh` check.** CI clones the repo into a temp dir, creates
   dirty state (modified + untracked files), runs the script through the full
   branch sequence with piped `yes`, and asserts each checkout lands on the
   expected branch with a clean tree.

## Risks And Mitigations

1. **`feature/gazelle` is a moving target; the workshop can break without any
   change of its own.** Mitigation: the `git_override` pins a specific commit,
   never the branch head. Updating the pin is a deliberate maintenance task
   accompanied by a full CI run of all branches (Testing Strategy item 1).
2. **Interactive terminal behavior of `bazel run` + Spectre.Console differs
   across platforms and terminals** (e.g. piping, IDE consoles, Windows
   terminals). Mitigation: the plain-Console UI is used until exercise 002 so
   first success never depends on Spectre; prerequisites demand a real
   terminal; the smoke run (Testing Strategy item 5) exercises piped input;
   the READMEs document known limitations.
3. **`next_exercise.sh` destroys uncommitted participant work.** Mitigation:
   the script prints a loud warning and requires typing `yes`; the main README
   repeats the warning and explains that **committed** work survives
   (`git reset --hard` resets to HEAD, so local commits are kept) while only
   uncommitted work is lost, and suggests committing experiments locally or
   using `git stash`.
4. **Version drift of registry deps (`bazel_gazelle`, `rules_go`,
   Spectre.Console, Newtonsoft.Json).** Mitigation: all versions are pinned in
   the reference solutions and in `nuget_resolve.json`; CI exercises the exact
   pinned set, so an upstream yank or incompatibility surfaces as a CI failure
   rather than a broken participant experience.

## Decision Summary

- Standalone git repo `dotnet_workshop/`; EngFlow-style: one branch per
  exercise, destructive `next_exercise.sh`, per-exercise READMEs with numbered
  steps, `<details>` hints (full reference solutions) and reflection questions.
- Five content branches: `main`, `exercise-000-intro`,
  `exercise-001-hello-bazel`, `exercise-002-nuget-resolve`,
  `exercise-003-gazelle`, `exercise-004-change`, in that fixed sequence.
- `rules_dotnet` consumed via `git_override` to
  `https://github.com/juulhobert/rules_dotnet.git`, pinned to a specific
  commit on `feature/gazelle` (not the branch head).
- Only prerequisites: bazelisk, git, a POSIX shell, internet, a real terminal.
  All workshop content in English.
- Given C# app: `TicTacToe.Engine` (Board, Player, ComputerPlayer; no deps)
  and `TicTacToe.Console` (Program.cs). The Console has two given versions:
  plain `System.Console` (exercises 000–001) and Spectre.Console (002
  onward); exercise 004 adds given `HighScores.cs` + updated `Program.cs`.
- Single target framework `net10.0`; packages pinned: Spectre.Console 0.49.1,
  Newtonsoft.Json 13.0.3; lock file `//:nuget_resolve.json` created with
  `REPIN=1 bazel run @nuget.net10.0//:pin` and checked in.
- Exercise 001 uses one handwritten `csharp_binary` `app` globbing both
  source dirs; exercise 003 replaces it with Gazelle-generated
  `//src/TicTacToe.Engine:tictactoe_engine` (library) and
  `//src/TicTacToe.Console:program` (binary) plus a root
  `alias(name = "app", ...)` so `bazel run //:app` remains the verification
  command throughout.
- Gazelle setup: `gazelle_binary` with `@rules_dotnet//dotnet/gazelle`, root
  directive `# gazelle:dotnet_target_frameworks net10.0`,
  `extra_args = ["-dotnet_nuget_lock_file=nuget_resolve.json", "."]`;
  requires `bazel_dep`s on `bazel_gazelle` and `rules_go`.
- Verification after every exercise (except 000) is `bazel run //:app`
  playing tic-tac-toe; exercises 000, 002 and 004 deliberately start broken.
- Out of scope: tests, multi-framework, F#/web/publish, WORKSPACE, advanced
  Gazelle, private feeds, teaching C#.

## Success Criteria

- A participant with only the listed prerequisites can clone the repo and
  complete all five exercises end-to-end using only the READMEs and hints.
- Every exercise (except 000) ends with `bazel run //:app` playing
  tic-tac-toe; exercises 002 and 004 visibly demonstrate the two dependency
  kinds (external NuGet, internal library) and the iteration loop.
- After exercise 003 the participant can explain how Gazelle derived every
  `deps` entry; after exercise 004 they have run the complete
  edit-MODULE → repin → regazelle → run loop themselves.
- All reference solutions are CI-verified on every branch, so the hints can
  never strand a participant.
