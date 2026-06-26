# FileAudit

Stateless end-to-end file auditing (read/parse/decode/unpack); reports only

Repository: `https://github.com/der-pepe/FileAudit`

## Main goals

- Stateless end-to-end file auditing (read/parse/decode/unpack) — reports only.
- Surface unreadable blocks, truncation/parse errors, container checksum failures, and
  media decode defects.
- The JSONL report is the only artifact — no stored manifests, hashes, or DB.
- Plugin-per-format design; cross-platform CLI + library, no UI in v0.1.

## How agents should use this memory

- Start with this file, [[current-status]], [[instructions/agent-rules]], and [[tasks/lessons]].
- Use [[context-map]] to pick only the relevant docs for the task.
- Check [[environment]] before suggesting shell commands.
- Create one file per active task under `tasks/` (parallel tasks supported).
- Use [[tasks/todo]] as the durable backlog only.

## Instructions

- [[instructions/agent-rules]]
- [[instructions/cli-tooling]]
- [[context-map]]

## Task tracking

- [[tasks/todo]] — durable backlog by priority
- `tasks/<task-name>.md` — one file per active task
- `tasks/done/` — completed task files
- [[tasks/lessons]] — correction patterns and recurring mistakes
- [[tasks/task-template]] — reusable task note template

## Main documents

- [[current-status]]
- [[environment]]
- [[sketchpad]] — scratch capture for raw ideas (NOT durable; do not act on without promotion)

- [[design-principles]] — stateless design, non-goals, safety rules
- [[engine-rules]] — verifier/checks/reporting/taxonomy rules
- [[contract]] — stable CLI + JSONL schema + event taxonomy + exit codes
- [[matching_rules]] — verifier selection and deterministic ordering
- [[verifiers]] — verifier intent and limitations
