# FileAudit contract (v0.1)

This document defines FileAudit’s stable CLI behavior, JSONL output schema, event taxonomy, and exit codes.

FileAudit is **stateless**: it does not store manifests, hashes, or per-file metadata. The report is the artifact.

---

## CLI

### Command
```bash
fileaudit scan <path...> [options]
```

`<path...>` can be files and/or directories. Directories are scanned recursively.

### Options (v0.1)

#### Output
- `--out <path>`
  Output path for the JSONL report.
  Default: `fileaudit-report.jsonl`

#### Mode
- `--mode audit|read`
  - `audit` (default): run format-aware plugins (ffmpeg/zip/sqlite/etc.)
  - `read`: run **BasicRead only** (full-byte streaming read), skip all plugins

#### Full-file read policy (audit mode only)
- `--read never|unmatched|on-fail|always`
  Default: `never`

Policy details:
- `never`
  Never run BasicRead.
- `unmatched`
  Run plugins first. If **no plugin matches**, run BasicRead (post).
- `on-fail`
  Run plugins first. If any plugin emits a `FAIL` (or throws), run BasicRead (post).
- `always`
  Run BasicRead **before** plugins (preflight).

#### Continue plugins after I/O read error
- `--plugins-after-read-error`
  Default: off

Only relevant when a BasicRead run fails (`--read=always` preflight or post-read).
If enabled, plugins will still run even after an I/O read error is recorded.

#### Tools discovery
- `--tools <dir>`
  Directory containing external tools (e.g., `ffmpeg.exe`). If omitted, tools are searched in PATH.

---

## Output format

FileAudit writes **JSONL**: one JSON object per line, one line per scanned file.

### Top-level record schema
Required fields:
- `file` (string): full path as scanned
- `utc` (string): ISO-8601 UTC timestamp
- `mode` (string): `audit` or `read`
- `status` (string): `OK` | `WARN` | `FAIL` | `SKIP`
- `verifiers_run` (array of string): ordered list of verifier names that ran
- `events` (array): list of events (may be empty)

Optional fields (present when relevant):
- `read_mode` (string): `never|unmatched|on-fail|always` (audit mode)
- `read_phase` (string): `pre|post|none`
- `read_performed` (bool)
- `io_failed` (bool)
- `plugins_ran_after_io_error` (bool)
- `coverage` (string): `semantic|fullread|none`

Notes:
- `coverage=semantic` means plugins ran (decode/unpack/parse); it does **not** guarantee every byte was read.
- `coverage=fullread` means BasicRead ran at least once for the file.
- `coverage=none` typically corresponds to `status=SKIP`.

### Event schema
Each `events[]` item has:
- `severity`: `INFO|WARN|FAIL`
- `kind`: stable coarse classification (see taxonomy)
- `code`: stable specific identifier (dot-style)
- `message`: human-readable message (may change between versions)
- `location`: optional string (see location conventions)
- `tool`: optional string (e.g., `basicread`, `ffmpeg`, `zip`, `sqlite`, `core`)

---

## Status computation (per file)

Status is computed from the highest event severity, with a special case for “no checks ran”.

- If any event has `severity=FAIL` → `status=FAIL`
- Else if any event has `severity=WARN` → `status=WARN`
- Else if no verifiers ran (or only a `core.no_verifier_matched` INFO) → `status=SKIP`
- Else → `status=OK`

`INFO` does not upgrade a file to WARN/FAIL.

---

## Exit codes

- `0` → no file has `status=WARN` or `status=FAIL` (SKIP allowed)
- `2` → at least one file is `WARN`, none are `FAIL`
- `3` → at least one file is `FAIL`
- `10` → run-level error (invalid args, cannot write report, unhandled exception)

---

## Event taxonomy (v0.1)

### Allowed `kind` values
- `IoReadError`
- `AccessDenied`
- `ExternalToolMissing`
- `ExternalToolFailed`
- `NoVerifierMatched`
- `ParseError`
- `CrcMismatch`
- `TrailingData`
- `DecodeWarning`
- `DecodeError`
- `ConcealmentDetected`
- `IntegrityCheckFailed`
- `UnsupportedFormat`

### Stable `code` strings (examples / initial set)

Core:
- `core.no_verifier_matched` (INFO)

I/O:
- `io.open_failed` (FAIL)
- `io.read_failed` (FAIL)
- `io.access_denied` (FAIL)

FFmpeg/media:
- `media.ffmpeg.missing` (INFO)
- `media.decode.aborted` (FAIL)
- `media.decode.invalid_container` (FAIL)
- `media.decode.warning` (WARN)
- `media.decode.concealment` (WARN)

ZIP/containers:
- `archive.zip.parse_failed` (FAIL)
- `archive.zip.entry_crc_mismatch` (FAIL)
- `archive.zip.entry_read_failed` (FAIL)

Images:
- `image.jpeg.trailing_data` (WARN)
- `image.png.trailing_data` (WARN)

SQLite:
- `db.sqlite.open_failed` (FAIL)
- `db.sqlite.integrity_failed` (FAIL)

---

## Location conventions

`location` is an optional string using `key=value` pairs.

Common patterns:
- I/O read errors: `offset=<bytes>`
- ZIP: `entry=<path/in/archive>`
- FFmpeg: `stream=<n> time=<HH:MM:SS.mmm>` (optional `frame=<n>`)
- Trailing data: `end=<offset> trailing=<bytes>`
- SQLite: `pragma=integrity_check`
