# Task: fail loudly on a missing input path (exit 10)

Branch: `feat/missing-input-exit10`

## Goal
`scan /does/not/exist` writes an empty report and exits 0 (silent false "all good").
Treat a missing input as a run-level error: stderr line per missing path, exit 10, no JSONL.

## Checklist
- [x] `InputPathNotFoundException` (Core) with `IReadOnlyList<string> Paths`.
- [x] `AuditEngine.ScanAsync`: validate provided inputs up front; throw if any missing.
- [x] `Program.cs`: catch it → stderr `Input path not found: <p>` per path, return 10.
- [x] Tests: missing → throws; mixed valid+missing → throws; valid still works.
- [x] `docs/contract.md`: exit-10 note for nonexistent input path.
- [x] build clean; tests green; smoke (missing → exit 10 no JSONL; real file unchanged).
