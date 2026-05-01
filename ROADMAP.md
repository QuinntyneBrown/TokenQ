# Roadmap

TokenQ is pre-1.0. The roadmap below reflects the existing requirements and
detailed design documents.

## Current

- Generate a TypeScript interface and matching Angular `InjectionToken`.
- Validate TypeScript identifier names.
- Write generated files to a requested directory.
- Refuse accidental overwrites unless `--force` is supplied.
- Normalise names from PascalCase, camelCase, or kebab-case input.
- Derive deterministic filenames with `.contract.ts` dotted suffixes for
  recognised types (`Store`, `Service`).
- Generate Angular barrel `index.ts` files via `tokenq provide`.
- Structured logging with `--verbose` debug detail.
- NuGet packaging and CI publish on every push to `main`.
- xUnit coverage for generator, CLI, validation, file output, barrel, and
  provide sub-command.

## Next

- Decide whether to support non-ASCII TypeScript identifiers.
- Consider atomic writes if users report partial-file failures.
- Consider `tokenq generate --name X` as a named sub-command alongside
  `provide` (breaking; revisit post-1.0).
- Consider a `--dry-run` flag for `provide` to preview barrel content without
  writing.

## Later

- Recursive barrel generation across multiple folders in one invocation.
- Add CI status badges to README.
