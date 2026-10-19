#!/usr/bin/env bash
# CI verification for the dotnet workshop repository.
#
# For every exercise branch (in a scratch clone):
#   1. Exercises 000, 002 and 004 must FAIL to build in their starting state
#      (the didactic motivation).
#   2. Apply the reference-solution patch from docs/solutions/ (kept on main).
#   3. bazel build //... must succeed with the solution applied.
#   4. Exercise 001: piped-input smoke run; a win/draw banner must appear.
#      (From exercise 002 onward the Spectre.Console SelectionPrompt needs an
#      interactive terminal, so those branches are verified with builds only.)
#   5. Exercises 003 and 004 solutions: `bazel run //:gazelle` must be
#      idempotent (no diff afterwards).
#   6. Exercise 004 solution: `REPIN=1 bazel run @nuget.net10.0//:pin` must
#      leave the lock file unchanged (freshness).
# Finally: next_exercise.sh must walk the full branch sequence, discarding
# uncommitted state at every hop, and print the completion message at the end.
#
# Local runs on restricted machines may need:
#   export JAVA_HOME=<path to a readable JDK>
#   BAZEL_GAZELLE_FLAGS="--@rules_go//go/config:pure"   # if cgo headers are unreadable
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOLUTIONS="$REPO_ROOT/docs/solutions"
WORK="$(mktemp -d)"
BAZEL_GAZELLE_FLAGS="${BAZEL_GAZELLE_FLAGS:-}"

BROKEN_START_BRANCHES=(
    "exercise-000-intro"
    "exercise-002-nuget-resolve"
    "exercise-004-change"
)
GAZELLE_BRANCHES=(
    "exercise-003-gazelle"
    "exercise-004-change"
)
ALL_EXERCISES=(
    "exercise-000-intro"
    "exercise-001-hello-bazel"
    "exercise-002-nuget-resolve"
    "exercise-003-gazelle"
    "exercise-004-change"
)

contains() {
    local needle="$1"
    shift
    local item
    for item in "$@"; do
        if [[ "$item" == "$needle" ]]; then
            return 0
        fi
    done
    return 1
}

fail() {
    echo "CI FAILED: $*" >&2
    exit 1
}

git clone -q "$REPO_ROOT" "$WORK/repo"
cd "$WORK/repo"

for branch in "${ALL_EXERCISES[@]}"; do
    echo "=== $branch ==="
    git checkout -q "$branch"

    # Word splitting of $extra_flags is intentional (it holds 0 or 1 flags).
    extra_flags=""
    if contains "$branch" "${GAZELLE_BRANCHES[@]}"; then
        extra_flags="$BAZEL_GAZELLE_FLAGS"
    fi

    if contains "$branch" "${BROKEN_START_BRANCHES[@]}"; then
        if bazel build //... $extra_flags > /dev/null 2>&1; then
            fail "$branch: starting state should NOT build"
        fi
        echo "starting state fails to build (expected)"
    fi

    patch="$SOLUTIONS/$branch.patch"
    if [[ -f "$patch" ]]; then
        git apply "$patch"
        git add -A
        bazel build //... $extra_flags
        echo "reference solution builds"
    fi

    if [[ "$branch" == "exercise-001-hello-bazel" ]]; then
        smoke_out=""
        if ! smoke_out="$(printf '1\n2\n3\n4\n5\n6\n7\n8\n9\n' | bazel run //:app 2>&1)"; then
            echo "$smoke_out"
            fail "$branch: smoke run command itself failed"
        fi
        if ! grep -qE 'wins!|draw!' <<< "$smoke_out"; then
            echo "$smoke_out"
            fail "$branch: smoke run did not print a win/draw banner"
        fi
        echo "smoke run printed a win/draw banner"
    fi

    if contains "$branch" "${GAZELLE_BRANCHES[@]}"; then
        bazel run //:gazelle $extra_flags
        git diff --exit-code || fail "$branch: gazelle is not idempotent"
        echo "gazelle idempotent"
    fi

    if [[ "$branch" == "exercise-004-change" ]]; then
        REPIN=1 bazel run @nuget.net10.0//:pin
        git diff --exit-code || fail "$branch: lock file is stale"
        echo "lock file fresh"
    fi
done

echo "=== next_exercise.sh sequence ==="
git clone -q "$REPO_ROOT" "$WORK/sequence"
cd "$WORK/sequence"
git checkout -q main

expect_next() {
    local expected="$1"
    echo "dirty" >> README.md
    echo "junk" > junk.txt
    echo yes | ./next_exercise.sh > /dev/null
    local landed
    landed="$(git rev-parse --abbrev-ref HEAD)"
    [[ "$landed" == "$expected" ]] \
        || fail "next_exercise.sh landed on $landed, expected $expected"
    [[ -z "$(git status --porcelain)" ]] \
        || fail "next_exercise.sh left a dirty tree on $expected"
    echo "arrived at $expected with a clean tree"
}

expect_next exercise-000-intro
expect_next exercise-001-hello-bazel
expect_next exercise-002-nuget-resolve
expect_next exercise-003-gazelle
expect_next exercise-004-change

complete_out="$(echo yes | ./next_exercise.sh)"
grep -q "Workshop complete" <<< "$complete_out" \
    || fail "next_exercise.sh did not print the completion message"
[[ "$(git rev-parse --abbrev-ref HEAD)" == "exercise-004-change" ]] \
    || fail "next_exercise.sh moved away from the last exercise"
echo "workshop completion message printed"

echo "CI PASSED"
