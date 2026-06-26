# Matching rules (v0.1)

Stable verifier selection rules.

Order when multiple match:
1) image
2) zip
3) sqlite
4) ffmpeg

Unknown formats:
- `status=SKIP` + `core.no_verifier_matched`
- unless `--read=unmatched` triggers BasicRead

## Image
Extensions:
- `.png`, `.jpg`, `.jpeg`

Magic:
- PNG `\x89PNG\r\n\x1a\n`
- JPEG `\xFF\xD8\xFF`

## ZIP
Extensions:
- `.zip`, `.cbz`, `.epub`, `.docx`, `.xlsx`, `.pptx`

Optional magic:
- `PK\x03\x04`, `PK\x01\x02`, `PK\x05\x06`

## SQLite
Magic:
- `SQLite format 3\0`

Extensions:
- `.sqlite`, `.sqlite3`, `.db`

## FFmpeg
Extension allowlist (video/audio):
Video:
- `.mp4`, `.m4v`, `.mov`, `.mkv`, `.webm`, `.avi`, `.mpg`, `.mpeg`, `.m2ts`, `.ts`, `.mts`, `.flv`, `.wmv`, `.asf`, `.3gp`, `.3g2`
Audio:
- `.mp3`, `.m4a`, `.aac`, `.flac`, `.wav`, `.ogg`, `.opus`, `.wma`
