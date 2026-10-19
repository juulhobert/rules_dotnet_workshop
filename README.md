# Exercise 000 — It doesn't build yet

## Goal

Understand what Bazel needs before it can do anything. This branch contains a
complete tic-tac-toe game in plain C# — and not a single Bazel file.

## Steps

1. Look at the code. There are two components:
   - `src/TicTacToe.Engine/` — the game logic (`Board`, `Player`,
     `ComputerPlayer`). No dependencies outside the .NET base class library.
   - `src/TicTacToe.Console/Program.cs` — the game loop and UI. It uses the
     engine via `using TicTacToe.Engine;`.
2. Try to run the game:

   ```sh
   bazel run //:app
   ```

3. Ask Bazel what it knows about this directory:

   ```sh
   bazel query //...
   ```

## Expected outcome

Both commands fail. That is intentional — this exercise is the "before"
picture. Read the error messages carefully; they tell you exactly what is
missing.

<details>
<summary>Hint: why does this fail?</summary>

`bazel run //:app` fails with an error like:

```
ERROR: Bazel 'run' must be invoked from within a workspace (below a directory having a MODULE.bazel file).
```

Bazel builds *targets*, not source trees. A directory of C# files means
nothing to Bazel until you declare:

1. `MODULE.bazel` — the bzlmod module: its name and its dependencies (such as
   rules_dotnet, the Bazel rules for .NET).
2. A `BUILD.bazel` file containing at least one target, such as a
   `csharp_binary`.

Exercise 001 adds exactly those two files.

</details>

## Questions

1. *Why does Bazel refuse to build before any BUILD file exists?*
   <details><summary>Answer</summary>
   Bazel builds targets, not source trees. A target must be declared in a
   BUILD file before Bazel can build, query, or run it — sources alone are
   invisible to it.
   </details>
2. *What two files does Bazel need at minimum in a bzlmod project?*
   <details><summary>Answer</summary>
   `MODULE.bazel` (the module definition) and a `BUILD.bazel` with at least
   one target in it.
   </details>

## Next

Run `./next_exercise.sh` to move on to exercise 001.
