#!/usr/bin/env bash
# next_exercise.sh - move to the next exercise of the workshop.
#
# Present identically on every branch. DESTRUCTIVE: discards all uncommitted
# changes and untracked files in the working tree before checking out the
# next branch. Committed work survives.
set -euo pipefail

SEQUENCE=(
    "main"
    "exercise-000-intro"
    "exercise-001-hello-bazel"
    "exercise-002-nuget-resolve"
    "exercise-003-gazelle"
    "exercise-004-change"
)

current=$(git rev-parse --abbrev-ref HEAD)

next=""
for i in "${!SEQUENCE[@]}"; do
    if [[ "${SEQUENCE[$i]}" == "$current" ]]; then
        next="${SEQUENCE[$((i + 1))]:-}"
        break
    fi
done

if [[ -z "$next" ]]; then
    if [[ "$current" == "${SEQUENCE[-1]}" ]]; then
        echo "Workshop complete! You have finished the last exercise."
        exit 0
    fi
    echo "ERROR: '$current' is not a workshop branch." >&2
    echo "Check out one of: ${SEQUENCE[*]}" >&2
    exit 1
fi

cat <<EOF
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
WARNING: this script is DESTRUCTIVE.

All uncommitted changes and untracked files in this working tree will be
permanently lost (git clean -fdx + git reset --hard). Committed work is
safe: commit or 'git stash' anything you want to keep before continuing.
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

Next exercise: $next
EOF

answer=""
if ! read -r -p "Type 'yes' to continue: " answer; then
    answer=""
fi
if [[ "$answer" != "yes" ]]; then
    echo "Aborted."
    exit 1
fi

git clean -fdx
git reset --hard
git checkout "$next"

echo
echo "You are now on $next."
echo "Open README.md to start the exercise."
