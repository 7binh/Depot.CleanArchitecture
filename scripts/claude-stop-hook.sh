#!/usr/bin/env sh
# Claude Code Stop hook: block "done" until the quality gate passes.
input=$(cat)
case "$input" in *'"stop_hook_active":true'*|*'"stop_hook_active": true'*) exit 0 ;; esac
out=$("$(dirname "$0")/check.sh" 2>&1) && exit 0
printf '%s\n' "$out" | tail -40 >&2
echo "Quality gate failed. Fix the errors above, then finish." >&2
exit 2
