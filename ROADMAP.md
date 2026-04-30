# Roadmap

TokenQ is pre-1.0. The roadmap below reflects the existing requirements and
detailed design documents.

## Current

- Generate a TypeScript interface and matching Angular `InjectionToken`.
- Validate TypeScript identifier names.
- Write generated files to a requested directory.
- Refuse accidental overwrites unless `--force` is supplied.
- Provide xUnit coverage for generator, CLI, validation, and file output.

## Next

- Implement richer name normalization from `docs/detailed-designs/06-name-derivation/README.md`.
- Align generated filenames with the `.contract.ts` design where required.
- Add structured logging through `Microsoft.Extensions.Logging`.
- Add packaging verification tests for NuGet tool output.
- Add release automation for test, pack, and publish workflows.

## Later

- Decide whether to support non-ASCII TypeScript identifiers.
- Consider atomic writes if users report partial-file failures.
- Add CI status badges after a public CI workflow exists.
- Publish install instructions for NuGet.org once the package is released.
