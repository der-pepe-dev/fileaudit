# FileAudit

Stateless end-to-end file auditing (read/parse/decode/unpack). Reports only.

FileAudit scans datasets to surface:
- unreadable blocks / I/O failures (optional full-byte read)
- truncation and parse errors
- internal checksum failures (ZIP containers, etc.)
- media decode defects (including “self-healed” concealment signals), via FFmpeg

**Design goal:** no stored manifests/hashes/DB. The report is the artifact.

## Quick start

```bash
fileaudit scan D:\Archive --out report.jsonl
```

Add full-file reading when you want it:

```bash
fileaudit scan D:\Archive --read=always --out report.jsonl
```

## Docs
- `docs/contract.md` — CLI + JSONL schema + exit codes
- `docs/verifiers.md` — what each verifier does
- `docs/matching_rules.md` — how verifiers are selected
