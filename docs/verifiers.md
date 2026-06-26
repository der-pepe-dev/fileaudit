# Verifiers (v0.1)

This document describes what each verifier does, what it reports, and what it does **not** guarantee.

---

## Semantic vs full-byte coverage
- **Semantic checks** (decode/unpack/parse) do not guarantee that every byte of the file was read.
- **Full-byte checks** (BasicRead) stream the file to EOF and will catch I/O errors anywhere in the file.

---

## BasicRead verifier
**Name:** `basicread`  
Streams the file end-to-end.

Emits:
- `FAIL` `IoReadError` (`io.read_failed`, `io.open_failed`, `io.access_denied`)

---

## FFmpeg verifier
**Name:** `ffmpeg`  
Decode-to-null and parse stderr for curated defect patterns.

Emits:
- `INFO` `ExternalToolMissing` (`media.ffmpeg.missing`)
- `FAIL` `DecodeError` (`media.decode.aborted`, `media.decode.invalid_container`)
- `WARN` `DecodeWarning` / `ConcealmentDetected` (`media.decode.warning`, `media.decode.concealment`)

---

## Image verifier
**Name:** `image`  
Detects trailing bytes after the logical end of a PNG (`IEND`) or JPEG (`EOI`).

Emits:
- `WARN` `TrailingData` (`image.png.trailing_data`, `image.jpeg.trailing_data`)

---

## ZIP verifier
**Name:** `zip`  
Reads every entry stream (CRC validation during read).

Emits:
- `FAIL` `CrcMismatch` / `ParseError` (`archive.zip.*`)

---

## SQLite verifier
**Name:** `sqlite`  
Runs `PRAGMA integrity_check;`

Emits:
- `FAIL` `IntegrityCheckFailed` (`db.sqlite.integrity_failed`)
- `FAIL` open/parsing failures (`db.sqlite.open_failed`)
