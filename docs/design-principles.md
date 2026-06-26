# Design principles

FileAudit's core identity and the boundaries that keep it focused. Read at the start
of any feature work.

## What FileAudit is

A **stateless** file-integrity auditor for cold-storage / archival datasets. It scans
files using an optional full-byte read (BasicRead) to catch I/O errors anywhere in the
file, plus format-aware verifiers (plugins) that decode/unpack/parse and emit
structured defect events. A JSONL report (one line per file) is the **only** persistent
artifact — the report is the artifact.

## Non-goals (do not add)

- No manifests, no stored hashes, no per-file database.
- No repair, no rewrite/refresh, no dedup/compare.
- No background services or scheduling.
- No UI in v0.1.

## Hard safety rule

Never write to scanned files. FileAudit is read-only and reporting-only.

## Tech / build constraints

- Target framework: **net10.0** (cross-platform; keep CLI + core Windows/Linux clean).
- Primary IDE: Visual Studio 2026.
- v0.1 avoids heavy parallelism.

## Change-output requirements

When modifying code: prefer small explicit changes; provide full file contents for any
file changed (no partial snippets/ellipses); keep markdown/code fenced; do not invent
files that aren't required.
