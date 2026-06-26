# Lessons

Correction patterns and recurring mistakes. Append a dated entry after any user
correction or hard-won lesson. Newest first.

- 2026-06-26: Workflow rule (user, standing): never commit to `main`. For every change,
  branch `feat/<topic>`, commit there, push, and open a PR against `main`. Default from now on.
- 2026-06-26: Don't wrap a slow `dotnet build` together with `git stash --keep-index` in one
  Bash call — the build can exceed the timeout and the kill leaves a dangling stash. Commit
  first, verify the full build in its own call (timeout >= 300s), then push.

<!-- - YYYY-MM-DD: <what went wrong> -> <the rule to follow next time> -->
