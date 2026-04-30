# Changelog

All notable changes to TokenQ are documented in this file.

The format follows the spirit of Keep a Changelog, and this project uses
semantic versioning once published packages begin.

## Unreleased

### Added

- Root README with quick start, CLI reference, safety model, documentation map,
  and community links.
- Open source project documents: contributing guide, support policy, security
  policy, code of conduct, license, authors, notice, and roadmap.
- GitHub issue and pull request templates.
- NuGet tool package metadata for local packing and installation.

## 0.1.0 - Initial development

### Added

- .NET 8 CLI project.
- TypeScript interface and Angular `InjectionToken` generator.
- Required `--name` option with `-n` alias.
- Optional `--output`, `--force`, and `--verbose` flags.
- Safe file writer with directory creation and overwrite protection.
- TypeScript identifier validation.
- Console logging through `Microsoft.Extensions.Logging`.
- xUnit test suite covering generator, CLI, validator, and file writer behavior.
- Requirements and detailed design documentation under `docs/`.
