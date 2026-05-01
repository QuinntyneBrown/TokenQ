# TokenQ - Angular InjectionToken generator

TokenQ is a small .NET command-line tool that generates a TypeScript file
containing an exported interface and a matching Angular `InjectionToken`.

Use it when an Angular application needs repeatable contract files for
services, stores, managers, or any other injectable abstraction.

[Quick Start](#quick-start) | [CLI Reference](#cli-reference) | [Architecture](docs/detailed-designs/00-index.md) | [Requirements](docs/specs/L1.md) | [Contributing](CONTRIBUTING.md)

---

> One command turns a name into a deterministic Angular contract file with
> a typed `InjectionToken`. TokenQ keeps the generator small, local,
> testable, and safe by default.

## Why TokenQ

Angular projects often need the same pattern repeated across features:

1. Define a TypeScript interface.
2. Create an `InjectionToken` typed by that interface.
3. Name the file and token consistently.
4. Avoid overwriting existing work by accident.

TokenQ automates that narrow workflow. It does not scaffold Angular modules,
modify project files, or talk to the network. It reads CLI options, renders a
deterministic TypeScript file, and writes it to the requested directory.

## Quick Start

### Install from NuGet

```powershell
dotnet tool install --global TokenQ
tokenq --name EventStore --output ./generated
```

The command writes `generated/event.store.contract.ts`. To upgrade an existing
install, swap `install` for `update`.

### Run from source

```powershell
dotnet restore
dotnet run --project src/TokenQ -- --name EventStore --output ./generated
```

### Pack and install locally

```powershell
dotnet pack src/TokenQ/TokenQ.csproj -c Release -o ./.artifacts/packages
dotnet tool install --global TokenQ --add-source ./.artifacts/packages
```

## What It Generates

Input:

```powershell
tokenq --name EventStore --output ./generated
```

Output file: `generated/event.store.contract.ts`

```ts
import { InjectionToken } from '@angular/core';

export interface IEventStore {
}

export const EVENT_STORE = new InjectionToken<IEventStore>('EVENT_STORE');
```

From a single name, TokenQ derives three things deterministically:

- the **interface name** — PascalCase form of `--name`, prefixed with `I`
  (skipped if the supplied name is already in `I<Pascal>` form),
- the **`InjectionToken` constant and string** — SCREAMING_SNAKE_CASE of the
  interface name with the leading `I` removed,
- the **file name** — kebab-case, ending in `.contract.ts`. When the trailing
  PascalCase word is one of the recognised file types `Store` or `Service`,
  it is promoted to a dotted segment (`event.store.contract.ts`); otherwise
  the kebab name is used flat (`data-mode-controller.contract.ts`).

`--name` may be supplied in PascalCase (`EventStore`), camelCase
(`commandService`), or kebab-case (`data-mode-controller`).

## CLI Reference

```text
tokenq --name <name>
       [--output <directory>]
       [--force]
       [--verbose]
```

| Option | Alias | Description |
|--------|-------|-------------|
| `--name <name>` | `-n` | Required. PascalCase, camelCase, or kebab-case name. The interface name, token, and file name are derived from it. |
| `--output <directory>` | `-o` | Output directory. Defaults to the current working directory. |
| `--force` | `-f` | Overwrite the target file when it already exists. |
| `--verbose` | `-v` | Print detailed unexpected exception information. |
| `--help` | `-h` | Print command help. |
| `--version` | | Print the package version. |

Exit codes:

| Code | Meaning |
|------|---------|
| `0` | Generation succeeded. |
| `1` | User-correctable error, such as invalid input or a file collision. |
| `2` | Unexpected internal error. |

## Naming Rules

`--name` must be a non-empty string of:

- 1 to 200 characters.
- ASCII letters, digits, and the kebab separator `-` only.
- The first character must be an ASCII letter.
- No leading or trailing `-` and no consecutive `-`.
- The normalised PascalCase form must not be a TypeScript reserved word.
- The name must not be a bare recognised file-type word (`Store`, `Service`)
  on its own — a base portion is required.

Recognised file-type suffixes are exactly `Store` and `Service`. They are
matched against the trailing PascalCase word of the normalised name.

Examples:

| Input | File | Interface | Token |
|-------|------|-----------|-------|
| `EventStore` | `event.store.contract.ts` | `IEventStore` | `EVENT_STORE` |
| `commandService` | `command.service.contract.ts` | `ICommandService` | `COMMAND_SERVICE` |
| `data-mode-controller` | `data-mode-controller.contract.ts` | `IDataModeController` | `DATA_MODE_CONTROLLER` |
| `IUserAccountManager` | `user-account-manager.contract.ts` | `IUserAccountManager` | `USER_ACCOUNT_MANAGER` |
| `Logger` | `logger.contract.ts` | `ILogger` | `LOGGER` |

## Safety Model

TokenQ applies a few conservative filesystem rules:

- It creates the output directory when it does not exist.
- It refuses to overwrite existing files unless `--force` is supplied.
- It resolves output paths before writing.
- It writes UTF-8 bytes with deterministic `\n` line endings from the
  generator.
- It never executes or interprets user input as code.

## Project Status

TokenQ is pre-1.0 and published to nuget.org as
[`TokenQ`](https://www.nuget.org/packages/TokenQ). All six design slices —
generator core, CLI shell, file output, logging, distribution, and name
derivation — are complete. Every push to `main` runs the test suite, packs a
new version `0.1.<run-number>`, and publishes to nuget.org via GitHub
Actions.

## Documentation

- [Product idea](docs/idea.md)
- [L1 high-level requirements](docs/specs/L1.md)
- [L2 detailed requirements](docs/specs/L2.md)
- [Detailed design index](docs/detailed-designs/00-index.md)
- [Generator core design](docs/detailed-designs/01-generator-core/README.md)
- [CLI shell design](docs/detailed-designs/02-cli-shell/README.md)
- [File output design](docs/detailed-designs/03-file-output/README.md)
- [Logging design](docs/detailed-designs/04-logging/README.md)
- [Distribution design](docs/detailed-designs/05-distribution/README.md)
- [Name derivation design](docs/detailed-designs/06-name-derivation/README.md)
- [Roadmap](ROADMAP.md)

## Development

Prerequisites:

- .NET 8 SDK or later.
- Git.

Common commands:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/TokenQ -- --help
```

The solution is intentionally small:

```text
src/TokenQ/
  Program.cs        # CLI composition root
  Generator.cs      # pure TypeScript file generator
  NameValidator.cs  # name format validation (letters, digits, '-')
  FileWriter.cs     # safe local file writes

tests/TokenQ.Tests/
  *.cs              # xUnit acceptance and unit tests
```

## Community

- Read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.
- Use [SUPPORT.md](SUPPORT.md) for questions and issue-routing guidance.
- Report vulnerabilities through [SECURITY.md](SECURITY.md).
- Follow [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) in all project spaces.

## License

TokenQ is licensed under the [MIT License](LICENSE).

## Trademarks

This project may refer to third-party projects, products, or services. Those
names may be trademarks of their respective owners. TokenQ is not affiliated
with Angular, Google, Microsoft, or the .NET Foundation.
