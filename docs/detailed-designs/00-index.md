# TokenQ Detailed Designs — Index

TokenQ is a .NET 8 global tool that generates a single TypeScript source file
containing an empty `export interface` and a typed Angular `InjectionToken`
constant. It is small on purpose: one process, one job, no persistence, no
network. The designs below break the tool into vertical slices that each
deliver an observable behaviour and can be implemented with ATDD in a small
context.

## System Context

![C4 Context](diagrams/c4_context.png)

A developer invokes `tokenq` from a shell. The tool reads CLI options, writes a
file to the local filesystem, and emits log output to the console. There are no
network dependencies, no databases, and no other actors.

## Container View

![C4 Container](diagrams/c4_container.png)

The tool is a single self-contained .NET process. It depends only on the host
filesystem and the standard streams (stdout/stderr).

## Vertical Slice Map

The slices below are ordered for ATDD. Each is independently testable and
each ends in something the user can observe. A later slice depends on the
classes introduced by an earlier slice but does not require any earlier slice
to be feature-complete in order to begin.

| #  | Feature | Status | What the user can observe after this slice | L2 IDs |
|----|---------|--------|---------------------------------------------|--------|
| 01 | [Generator Core](01-generator-core/README.md) | Superseded by 06 | Calling `Generator.Render("IFooService")` returns the exact bytes that will be written to `foo-service.ts`. | L2-001, L2-002, L2-005, L2-007, L2-009, L2-015 |
| 02 | [CLI Shell](02-cli-shell/README.md) | Complete | `tokenq --name IFooService` parses arguments, calls the generator, prints content to stdout, and exits with the right code. | L2-003, L2-004 (option binding), L2-011, L2-016 |
| 03 | [File Output](03-file-output/README.md) | Complete | `tokenq --name IFooService --output ./svc` writes the file to disk safely with overwrite control. | L2-004 (write), L2-006, L2-008 |
| 04 | [Logging](04-logging/README.md) | Complete | Success and failure paths emit the right log levels to the right streams; `--verbose` reveals debug detail. | L2-010, L2-011 (verbose stack trace) |
| 05 | [Distribution](05-distribution/README.md) | Complete | `dotnet pack` produces a NuGet `.nupkg` that installs as a global tool and starts within the performance budget. | L2-012, L2-013, L2-014 |
| 06 | [Name Derivation](06-name-derivation/README.md) | Complete | `Generator.Render("EventStore")` yields `event.store.contract.ts` with `IEventStore` + `EVENT_STORE`; `commandService` and `data-mode-controller` work likewise. | L2-001, L2-002, L2-005, L2-007, L2-015, L2-017, L2-018 |
| 07 | [Barrel Generator](07-barrel-generator/README.md) | Complete | `BarrelGenerator.Render("dashboard-state", filenames)` returns the bytes of an Angular `index.ts` that re-exports classes/tokens/types and exposes a `provideDashboardState()` function. | L2-021, L2-022, L2-023, L2-024, L2-025, L2-026, L2-027, L2-028, L2-030 |
| 08 | [`provide` Sub-command](08-provide-subcommand/README.md) | Accepted | `tokenq provide --path ./dashboard-state` scans the folder, builds the barrel via slice 07, writes `index.ts` via the existing `FileWriter`. | L2-019, L2-020, L2-021 (I/O), L2-029, L2-030 |

Slice 06 refines slice 01: it changes how the generator turns a user-supplied
name into an interface name, token, and filename. Tests written against the
slice-01 contract (`foo-service.ts`, no auto-`I` prefix) will be replaced when
slice 06 lands.

Slices 07 and 08 add the `provide` sub-command. Slice 07 introduces a single
new pure class (`BarrelGenerator`); slice 08 wires it into the existing CLI
shell as a sub-command and reuses the existing `FileWriter` for the actual
write. No behavioural change to slices 01–06.

## Whole-Tool Code Map

The complete tool is intentionally tiny. Six C# files after slice 08, four of
them under 60 lines:

```
src/TokenQ/
  TokenQ.csproj         // net8.0, PackAsTool, NuGet metadata        [slice 5]
  Program.cs            // composition root + root + provide command [slice 2 + 8]
  Generator.cs          // pure: name -> (filename, content)         [slice 1 + 6]
  NameValidator.cs      // pure: validates TS identifier             [slice 1 + 6]
  FileWriter.cs         // safe write with overwrite + path checks   [slice 3]
  BarrelGenerator.cs    // pure: folder + names -> index.ts content  [slice 7]
```

Logging (slice 4) is configuration — it adds DI registrations and message calls
to the existing classes rather than introducing new ones. Slice 08 adds one
DI registration (`BarrelGenerator`) and one new sub-command on the existing
root.

## ATDD Convention

Every acceptance test file in `tests/TokenQ.Tests/` must carry a header
identifying the L2 requirement(s) it covers, e.g.:

```csharp
// Acceptance Test
// Traces to: L2-001, L2-002
// Description: Generated content contains interface and InjectionToken
```

This is enforced by convention only; reviewers should reject tests without it.
