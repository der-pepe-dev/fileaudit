# FileAudit — current status

_Update only when durable project status changes: major feature completed, known
limitation discovered, milestone changed, or durable architectural direction changed.
Prefer appending dated notes over rewriting._

## Status

v0.1 — stateless file-integrity auditor. CLI (`fileaudit scan`) producing a JSONL
report. Engine runs an optional full-byte BasicRead plus format-aware verifier plugins
(BasicRead, FFmpeg, SQLite, Zip). The CLI/JSONL/taxonomy/exit-code contract is defined
and treated as stable. No UI; cross-platform CLI + core.

## Known limitations

- v0.1 deliberately avoids heavy parallelism.
- No manifests/hashes/DB by design (stateless).
- FFmpeg is an optional dependency (INFO `media.ffmpeg.missing` when absent).

## Recent notes

<!-- Append dated notes here, newest first: -->
- 2026-06-27: Added first test project `tests/FileAudit.Tests` (TUnit on
  Microsoft.Testing.Platform, net10.0), in the solution and run by CI (`dotnet test`). MTP's
  new `dotnet test` mode is opted into via `global.json` `test.runner` (positional sln no
  longer works — use `--solution`). 13 tests lock the contract: status
  (OK/WARN/FAIL/SKIP incl. the two PR #1 regressions — clean fullread → OK, unmatched+clean
  fallback read → OK), exit-code max across files, and JSONL shape (snake_case keys, string
  enums, null optionals omitted, `read_mode` `on-fail` token, `read_mode` omitted in read mode).
  Test infra: `FakeVerifier`, `CapturingReporter`, `TempFile` in TestDoubles.cs.
- 2026-06-26: Pushed to github.com/der-pepe-dev/fileaudit. Repo did not build on
  arrival: bumped `Microsoft.Data.Sqlite` 9.0.0→10.0.9 and pinned patched
  `SQLitePCLRaw.bundle_e_sqlite3` 3.0.3 (NU1903 GHSA-2m69-gcr7-jv3q); fixed 12 compile
  errors (yield-in-catch in BasicRead/SQLite/Zip, FFmpeg arg-quote bug, stackalloc-in-loop
  in PngTrailingData, ref-span-in-LINQ in AuditEngine). Build now clean.
- 2026-06-26: `JsonlReporter` now honors the JSONL contract via `ContractJson.Options`
  (snake_case keys, string enums OK/WARN/FAIL/SKIP + INFO/WARN/FAIL, mode audit/read,
  DefectKind PascalCase, null optionals omitted, relaxed encoder). Previously emitted
  PascalCase keys + int enums — a stable-contract violation. Verified against skip/read/fail.
- 2026-06-26: Fixed status mis-classification: clean full read reported `status=SKIP`
  instead of `OK`. `AuditEngine.Build` SKIP guard now requires `events.Count > 0` before the
  all-`NoVerifierMatched` test, so a verifier that ran with zero findings → `OK`. Verified:
  read-mode clean → OK; audit no-match → SKIP.<!-- - YYYY-MM-DD: ... -->
