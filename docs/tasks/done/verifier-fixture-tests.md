# Task: verifier tests against real fixtures (Zip / SQLite / BasicRead)

Branch: `feat/verifier-fixture-tests`

## Goal
Real `ZipVerifier`/`SqliteVerifier`/`BasicReadVerifier` logic is untested (all prior tests use
`FakeVerifier`). Add in-code fixtures (no committed binaries). Tests-only; no product change.

## Checklist
- [x] Test refs: Zip, SQLite, BasicRead plugins.
- [x] `TempPath` helper (unique temp path, deletes on dispose, no pre-write).
- [x] Zip: clean → none; garbage .zip → parse_failed; truncated → parse_failed; corrupt entry → entry crc/read fail (tolerant).
- [x] SQLite: clean → none; garbage .db → open_failed; corrupted page → integrity/open fail (tolerant).
- [x] BasicRead: clean → none; chmod 000 → io.access_denied.
- [x] build clean; `dotnet test` green.
