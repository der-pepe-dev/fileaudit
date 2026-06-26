# Lessons

Correction patterns and recurring mistakes. Append a dated entry after any user
correction or hard-won lesson. Newest first.

- 2026-06-26: Workflow rule (user, standing): never commit to `main`. For every change,
  branch `feat/<topic>`, commit there, push, and open a PR against `main`. Default from now on.
- 2026-06-27: TUnit tests on .NET 10 SDK: the VSTest bridge is gone, so `dotnet test` fails
  with "Testing with VSTest target is no longer supported". Opt into MTP mode via `global.json`
  `{ "test": { "runner": "Microsoft.Testing.Platform" } }` — NOT `dotnet.config`, and remove
  `TestingPlatformDotnetTestSupport` (that's the old bridge). In MTP mode positional sln paths
  break (`dotnet test FileAudit.sln` leaks args to the test app → "Zero tests ran"); use bare
  `dotnet test` (auto-discover) or `--solution`/`--project`.
- 2026-06-27: Workflow rule (user, standing): after fixing a PR review comment, reply on that
  specific comment thread (REST `pulls/{n}/comments/{id}/replies`) AND mark the thread resolved
  (GraphQL `resolveReviewThread`). A single summary PR comment is not enough — reply + resolve
  per thread.
- 2026-06-26: Don't wrap a slow `dotnet build` together with `git stash --keep-index` in one
  Bash call — the build can exceed the timeout and the kill leaves a dangling stash. Commit
  first, verify the full build in its own call (timeout >= 300s), then push.

<!-- - YYYY-MM-DD: <what went wrong> -> <the rule to follow next time> -->
