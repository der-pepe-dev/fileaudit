# Task: ImageVerifier (wire PNG/JPEG trailing-data checks)

Branch: `feat/image-verifier`

## Goal
`PngTrailingData`/`JpegTrailingData`/`TailScan` are implemented but unwired; the contract
codes `image.png.trailing_data` / `image.jpeg.trailing_data` (WARN) can never be emitted.
Add an `ImageVerifier` plugin, register it, sync stable docs. No check-logic change.

## Checklist
- [x] New project `src/FileAudit.Plugins.Image` (mirror Zip plugin), add to solution.
- [x] `ImageVerifier`: Name `image`, Order 5; CanVerify by ext (.png/.jpg/.jpeg) or magic;
      VerifyAsync classifies PNG/JPEG and maps `CheckFinding` → WARN `TrailingData` event.
- [x] Register in `Program.cs` + Cli ProjectReference.
- [x] Tests: PNG clean/trailing, JPEG clean/trailing, CanVerify ext+magic (ref plugin from test proj).
- [x] Docs: `verifiers.md` Image section; `matching_rules.md` order + Image extensions/magic.
- [x] `dotnet build` clean; `dotnet test` green; manual smoke (PNG+trailing → WARN, exit 2).
