# TokenQ - Angular InjectionToken generator

TokenQ is a small .NET command-line tool that generates a TypeScript file
containing an exported interface and a matching Angular `InjectionToken`.

Use it when an Angular application needs repeatable contract files for
services, stores, managers, or any other injectable abstraction.

[Quick Start](#quick-start) | [CLI Reference](#cli-reference) | [Architecture](docs/detailed-designs/00-index.md) | [Requirements](docs/specs/L1.md) | [Contributing](CONTRIBUTING.md)

---

> One command turns an interface name into a deterministic Angular token file.
> TokenQ keeps the generator small, local, testable, and safe by default.

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

### Run from source

```powershell
dotnet restore
dotnet run --project src/TokenQ -- --name IFooService --output ./generated
```

The command writes `generated/foo-service.ts`.

### Pack and install locally

```powershell
dotnet pack src/TokenQ/TokenQ.csproj -c Release -o ./.artifacts/packages
dotnet tool install --global TokenQ --add-source ./.artifacts/packages
tokenq --name IFooService --output ./generated
```

If `TokenQ` is already installed, use `dotnet tool update --global TokenQ
--add-source ./.artifacts/packages`.

## What It Generates

Input:

```powershell
tokenq --name IFooService --output ./generated
```

Output file: `generated/foo-service.ts`

```ts
import { InjectionToken } from '@angular/core';

export interface IFooService {
}

export const FOO_SERVICE = new InjectionToken<IFooService>('FOO_SERVICE');
```

The current generator preserves the interface name supplied with `--name`.
When deriving the file name and token constant, a leading `I` is removed only
when it is followed by an uppercase letter.

## CLI Reference

```text
tokenq --name <interface-name>
       [--output <directory>]
       [--force]
       [--verbose]
```

| Option | Alias | Description |
|--------|-------|-------------|
| `--name <interface-name>` | `-n` | Required TypeScript interface name. |
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

`--name` must be a valid TypeScript identifier:

- 1 to 200 characters.
- No whitespace.
- Must begin with an ASCII letter, `_`, or `$`.
- May contain ASCII letters, digits, `_`, or `$`.
- Must not be a TypeScript reserved word.

Examples:

| Input | File | Token |
|-------|------|-------|
| `IFooService` | `foo-service.ts` | `FOO_SERVICE` |
| `IUserAccountManager` | `user-account-manager.ts` | `USER_ACCOUNT_MANAGER` |
| `Logger` | `logger.ts` | `LOGGER` |

## Safety Model

TokenQ applies a few conservative filesystem rules:

- It creates the output directory when it does not exist.
- It refuses to overwrite existing files unless `--force` is supplied.
- It resolves output paths before writing.
- It writes UTF-8 bytes with deterministic `\n` line endings from the
  generator.
- It never executes or interprets user input as code.

## Project Status

TokenQ is pre-1.0. The implemented tool covers the current generator, CLI, file
writer, validation, console logging, and test suite. The requirements and
detailed designs also document planned work, including richer name derivation
and release automation.

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
  NameValidator.cs  # TypeScript identifier validation
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
