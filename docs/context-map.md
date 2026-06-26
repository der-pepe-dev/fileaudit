# FileAudit context map

Use this file to decide which memory files to read for a task. Do not read every
file by default.

## Always read at session start

- [[index]]
- [[current-status]]
- [[environment]]
- [[design-principles]]
- [[instructions/agent-rules]]
- [[tasks/lessons]]

## Engine / verifiers / checks / reporting

Read when implementing or changing a verifier, check, the engine, or the JSONL reporter.

- [[engine-rules]]
- [[contract]] — authoritative CLI/JSONL/taxonomy/exit-code contract
- [[matching_rules]] — verifier selection + deterministic ordering
- [[verifiers]] — verifier intent and limitations

## Stable contracts (read before changing behavior)

Any behavior change touching CLI flags, JSONL schema, event taxonomy, status, or exit
codes MUST update these in the same change.

- [[contract]]
- [[matching_rules]]
- [[verifiers]]

## Coding / tooling (always relevant when writing code)

- [[design-principles]] — change-output requirements, build constraints
- [[instructions/cli-tooling]]
