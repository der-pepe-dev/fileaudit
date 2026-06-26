# CLI tooling

Fast CLI tools preferred over slower POSIX equivalents in shell pipelines. Check
`environment.md` for which are actually installed on the current host.

- `rg` (ripgrep) — code/text search, instead of `grep -r`.
- `fd` — file finding, instead of `find`.
- `jq` — JSON querying (CLI output, config, lockfiles).
- `yq` — YAML query/validate (e.g. CI workflow files).
- `delta` — readable git diffs (`git -c core.pager=delta diff`).
- `hyperfine` — command benchmarking (before/after timing).

Use dedicated editor/search tools when available; reach for these in shell pipelines.

- `ffmpeg` / `ffprobe` — media decode verifier backend (FileAudit.Plugins.FFmpeg).
  Optional: absence emits INFO `media.ffmpeg.missing`, never fails the run.
- `sqlite3` — inspect SQLite containers when debugging the SQLite plugin.
- `unzip` / `zipinfo` — inspect ZIP containers when debugging the Zip plugin.
