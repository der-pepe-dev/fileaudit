# Backlog

Durable, prioritized task list. Active work goes in `tasks/<task-name>.md`, not here.

## High priority

- **Wire image trailing-data checks (ImageVerifier).** `PngTrailingData`,
  `JpegTrailingData`, and `TailScan` exist in `FileAudit.Checks` but are referenced by
  nothing — no verifier emits `image.png.trailing_data` / `image.jpeg.trailing_data`, so
  those documented contract codes are currently dead. Add an `ImageVerifier` plugin that
  runs the checks for PNG/JPEG and register it in `Program.cs`.
- **Verifier behavior tests against real fixtures.** Current tests use `FakeVerifier`; the
  actual Zip/SQLite/BasicRead/(image) logic is untested. Add fixtures and tests: ZIP (clean,
  CRC-corrupt, truncated), SQLite (clean, integrity-failed), PNG/JPEG (clean, trailing data),
  BasicRead (clean, truncated/unreadable). Keep fixtures tiny and committed.

## Medium priority

- **Missing input paths are silent.** `AuditEngine.ExpandInputs` skips a path that is neither
  a file nor a directory, so `fileaudit scan does-not-exist` writes an empty report and exits
  `0`. Decide the contract: per-path FAIL record, or run-level error (exit `10`). Update
  `docs/contract.md` and tests in the same change.
- **`MaxEventsPerFile` truncation is silent.** The plugin loop breaks at the cap without any
  marker. Consider emitting an INFO (e.g. `core.events_truncated`) so consumers know the event
  list was clipped.
- **FFmpeg `location` not populated.** Contract location conventions specify
  `stream=<n> time=<HH:MM:SS.mmm>` for media, but `FFmpegVerifier` emits no location. Parse
  stderr for it. Needs an ffmpeg-present integration test (optional dependency).
- **Directory enumeration robustness.** `Directory.EnumerateFiles(..., AllDirectories)` —
  define behavior for symlinks, per-directory access-denied, and result ordering determinism.

## Low priority / someday

- Parallel scanning (explicitly deferred in v0.1).
- Additional formats: gzip/tar, zip-based office docs, PDF.
- Publish a JSON Schema for the JSONL record and sample reports under `docs/`.
- Single-file / AOT publish of the CLI; packaging.
