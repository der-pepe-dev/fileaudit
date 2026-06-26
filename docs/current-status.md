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
<!-- - YYYY-MM-DD: ... -->
