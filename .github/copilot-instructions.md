# GitHub Copilot Instructions — FileAudit

## Project summary
FileAudit is a **stateless** file integrity auditor. It scans files end-to-end using:
- optional **full-byte read** (BasicRead) to detect I/O read errors anywhere in the file
- **format-aware verifiers** (plugins) that decode/unpack/parse files and emit structured defect events
- a **JSONL report** (one line per file) as the only persistent artifact

**Non-goals:**
- No manifests, no stored hashes, no per-file database
- No repair, no rewrite/refresh, no dedup/compare
- No background services or scheduling

## Contracts you MUST follow
- `docs/contract.md` defines stable CLI flags, JSONL schema, event taxonomy, status computation, and exit codes.
- `docs/matching_rules.md` defines verifier selection and deterministic ordering.
- `docs/verifiers.md` describes verifier intent and limitations.

If you change behavior that impacts these contracts, update the docs in the same PR.

## Architecture conventions
### Projects
- `FileAudit.Abstractions` — stable DTOs/interfaces (`DefectEvent`, `FileReport`, `IVerifier`, enums)
- `FileAudit.Checks` — reusable pure checks (stream-in / finding-out). No CLI, no Process, no logging.
- `FileAudit.Core` — engine orchestration + JSONL reporter
- `FileAudit.Plugins.*` — glue from checks/tools to `DefectEvent`
- `FileAudit.Cli` — command parsing, wiring, exit codes

### Verifiers
- Implement `IVerifier`.
- `CanVerify(path, header)` must be **cheap**: extension allowlist and/or header sniff only.
- `VerifyAsync` must:
  - avoid buffering whole files
  - emit `DefectEvent` instead of throwing for expected corruption
  - be cancellation-aware (`CancellationToken`)
- Use deterministic `Order` values to maintain stable execution order.

### Checks (reusable)
- Checks live in `FileAudit.Checks.*`.
- Checks accept `Stream` or `path` and return `CheckFinding` results.
- Checks must not: print, spawn processes, read env vars, or depend on CLI flags.

## Reporting rules
- Output is JSONL: one object per file (`FileReport`).
- File status rules:
  - FAIL if any FAIL event
  - WARN if any WARN event and no FAIL
  - SKIP if no verifiers ran (or only `core.no_verifier_matched`)
  - OK otherwise
- Exit codes:
  - 0 = OK/SKIP only
  - 2 = WARN present
  - 3 = FAIL present
  - 10 = run-level error

## Event taxonomy
Use stable `kind` and `code` values from `docs/contract.md`.
Avoid inventing new codes casually—if needed, add to the contract docs.

## FFmpeg verifier guidance
- v0.1 uses conservative pattern matching on stderr.
- Do NOT treat every "error" word as failure; maintain a curated list.
- If tool not found, emit `media.ffmpeg.missing` (INFO) and do not fail the run.

## Read policies
- `--read=always` performs BasicRead **before** plugins.
- `--read=unmatched` and `--read=on-fail` perform BasicRead **after** plugins.
- `--plugins-after-read-error` allows plugins to run even after a BasicRead I/O error.

Maintain these semantics exactly as defined in the contract.

## Performance and safety
- Avoid excessive parallelism in v0.1.
- Use streaming APIs and bounded buffers.
- Never write to user files; this project is read-only and reporting-only.

## How to add a new verifier (checklist)
1. Add `src/FileAudit.Plugins.<Name>/`
2. Implement `IVerifier` with cheap `CanVerify`
3. Pick an `Order` that preserves stable run ordering
4. Emit only contract-compliant `kind`/`code`
5. Update docs if behavior/codes change
