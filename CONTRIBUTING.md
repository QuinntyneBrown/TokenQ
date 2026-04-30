# Contributing to TokenQ

Thanks for helping improve TokenQ. This project is small, so contributions are
expected to stay focused, tested, and aligned with the existing design docs.

## Code of Conduct

All participation is covered by [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## Development Setup

Prerequisites:

- .NET 8 SDK or later.
- Git.

From the repository root:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/TokenQ -- --help
```

## Project Layout

```text
src/TokenQ/          # CLI implementation
tests/TokenQ.Tests/  # xUnit tests
docs/specs/          # L1 and L2 requirements
docs/detailed-designs/
```

Before changing behavior, read the relevant requirement and detailed design.
If the desired behavior is not documented, update the docs in the same change.

## Pull Request Guidelines

- Keep pull requests narrowly scoped.
- Include tests for behavior changes.
- Preserve deterministic output and LF line endings in generated files.
- Avoid new dependencies unless they remove meaningful complexity.
- Do not mix formatting-only changes with behavior changes.
- Update `CHANGELOG.md` under `Unreleased` for user-visible changes.

## Acceptance Test Convention

Acceptance tests should identify the requirements they cover with a header:

```csharp
// Acceptance Test
// Traces to: L2-001, L2-002
// Description: Generated content contains interface and InjectionToken
```

This keeps tests connected to the requirements in `docs/specs/L2.md`.

## Commit and Branch Style

Use short, imperative commit subjects:

```text
Add force overwrite test
Document local tool install flow
```

There is no required branch naming scheme.

## Release Checklist

1. Update `CHANGELOG.md`.
2. Confirm `dotnet test` passes.
3. Build a release package:

   ```powershell
   dotnet pack src/TokenQ/TokenQ.csproj -c Release -o ./.artifacts/packages
   ```

4. Smoke test the package with a local tool install.
5. Tag the release after the package is verified.

## Security Issues

Do not report vulnerabilities in public issues. Follow
[SECURITY.md](SECURITY.md).
