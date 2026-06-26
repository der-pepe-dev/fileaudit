# Task: add FileAudit.Tests (lock the contract)

Branch: `feat/tests`

## Goal
First test project. Lock down the stable JSONL/exit-code contract so future changes
can't silently drift it again.

## Checklist
- [x] Scaffold `tests/FileAudit.Tests` (TUnit on Microsoft.Testing.Platform, net10.0), add to solution.
- [x] Test infra: fake `IVerifier` (emit chosen events) + capturing `IReporter`.
- [x] Status computation: OK / WARN / FAIL / SKIP, incl. clean fullread → OK and
      no-match-no-read → SKIP (the two bugs fixed in PR #1).
- [x] Exit codes: 0 (OK/SKIP), 2 (WARN), 3 (FAIL); max across files.
- [x] JSONL shape via `JsonlReporter`: snake_case keys, string enums
      (status OK/WARN/FAIL/SKIP, severity INFO/WARN/FAIL, mode audit/read,
      kind PascalCase), null optionals omitted, `read_mode` token `on-fail`.
- [x] `dotnet test` green (MTP mode; 13 passed).

## Notes
- `ContractJson` is internal — assert JSON via `JsonlReporter` writing a temp file,
  not by referencing the serializer directly.
- Test csproj: set `TreatWarningsAsErrors=false` (TUnit analyzer warnings shouldn't
  fail the build; product code keeps warnings-as-errors).
- TUnit runs on Microsoft.Testing.Platform. .NET 10 SDK dropped the VSTest bridge, so
  the new `dotnet test` MTP mode is opted into via `global.json` `test.runner`
  (not `dotnet.config`, not `TestingPlatformDotnetTestSupport`). Positional sln/project
  paths are gone — use `--solution` / `--project`; bare `dotnet test` auto-discovers.
