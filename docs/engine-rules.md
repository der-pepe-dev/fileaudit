# Engine rules

The rules governing verifiers, checks, reporting, and the event taxonomy. Read when
implementing or changing any verifier, check, or the engine/reporter. The authoritative
contract is [[contract]]; this doc is the working summary.

## Repo structure (follow it)

- `src/FileAudit.Abstractions` — stable DTOs/interfaces (`DefectEvent`, `FileReport`,
  `IVerifier`, enums).
- `src/FileAudit.Checks` — reusable pure checks (stream-in / finding-out). No CLI, no
  Process, no logging.
- `src/FileAudit.Core` — engine orchestration + JSONL reporter.
- `src/FileAudit.Plugins.*` — glue from checks/tools to `DefectEvent`.
- `src/FileAudit.Cli` — command parsing, wiring, exit codes.

## Verifier rules

- Implement `IVerifier`.
- `CanVerify(path, header)` must be **cheap**: extension allowlist and/or small header
  sniff only.
- `VerifyAsync` must: stream/scan (avoid buffering whole files); emit `DefectEvent`
  instead of throwing for expected corruption; respect cancellation (`CancellationToken`).
- Use deterministic `Order` values for stable ordering.

## Checks rules (reusable)

- Checks live in `FileAudit.Checks.*`.
- Checks must be pure: no console output, no process spawning, no environment dependence.
- Accept `Stream` or `path`; return `CheckFinding` results.

## Reporting rules

Report is JSONL (`FileReport`). File status:
- FAIL if any FAIL event
- WARN if any WARN event and no FAIL
- SKIP if no verifiers ran (or only `core.no_verifier_matched`)
- OK otherwise

Exit codes (do not add more without updating [[contract]]):
- 0 = OK/SKIP only
- 2 = WARN present
- 3 = FAIL present
- 10 = run-level error

## Event taxonomy

Use stable `kind` and `code` values from [[contract]]. `kind` stays small/coarse;
`code` is dot-style and specific (e.g. `media.decode.aborted`). Do not invent new codes
casually; if needed, add them to the contract docs in the same change.

## External tools

FFmpeg is optional. If missing, emit INFO (`media.ffmpeg.missing`) and do not fail the
run.

## Contract stability

`docs/contract.md`, `docs/matching_rules.md`, and `docs/verifiers.md` are stable
contracts. If you change behavior that affects them, update the docs in the same change.
