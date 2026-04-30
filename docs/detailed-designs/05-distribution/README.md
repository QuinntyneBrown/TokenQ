# 05 - Distribution — Detailed Design

**Status:** Accepted

## 1. Overview

This slice ships the tool. After it lands, `dotnet pack` produces a NuGet
`.nupkg` that can be installed with `dotnet tool install --global TokenQ`,
and the resulting `tokenq` command starts inside the performance budget on a
clean .NET 8 host. There is no new code — the work is in the project file
and a small set of repository-level checks.

This slice has no diagrams. Everything that matters lives in `TokenQ.csproj`,
the published NuGet metadata, and a single performance acceptance test.

**In scope:** target framework, packaging properties, NuGet metadata,
performance acceptance test.

**Out of scope:** publishing to nuget.org (a separate operational step
performed once, manually), CI workflow.

**Traces to:** L2-012, L2-013, L2-014.

## 2. Project File

`src/TokenQ/TokenQ.csproj` — the single source of truth for distribution. The
final shape (with explanations inline) is:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- L2-012: target net8.0 exactly -->
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>

    <!-- L2-013: tool packaging -->
    <PackAsTool>true</PackAsTool>
    <ToolCommandName>tokenq</ToolCommandName>
    <PackageOutputPath>./nupkg</PackageOutputPath>

    <!-- L2-013: NuGet metadata required for publishability -->
    <PackageId>TokenQ</PackageId>
    <Version>0.1.0</Version>
    <Authors>Quinntyne Brown</Authors>
    <Description>
      Generates a TypeScript file containing an empty interface and an
      Angular InjectionToken from a single command.
    </Description>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/quinntyne/TokenQ</PackageProjectUrl>
    <RepositoryUrl>https://github.com/quinntyne/TokenQ</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageTags>angular;typescript;injection-token;codegen;dotnet-tool</PackageTags>
    <PackageReadmeFile>README.md</PackageReadmeFile>

    <!-- L2-013 #3: keep the package lean -->
    <IncludeSymbols>false</IncludeSymbols>
    <DebugType>embedded</DebugType>

    <!-- Surface InformationalVersion to System.CommandLine's --version handler -->
    <GenerateAssemblyInfo>true</GenerateAssemblyInfo>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.CommandLine" Version="2.0.0-*" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection"
                      Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console"
                      Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <None Include="..\..\README.md" Pack="true" PackagePath="\" />
  </ItemGroup>

</Project>
```

Notes on choices:

- **`net8.0`, not `net8.0;net9.0`.** L2-012 says target `net8.0`; running on
  newer .NET versions is handled by the .NET host's roll-forward. No
  multi-targeting matrix today.
- **Single `ToolCommandName = tokenq`.** Stable, lowercase, easy to type.
- **`Version = 0.1.0`.** Pre-1.0; bumped manually until release automation
  exists.
- **`IncludeSymbols = false`, `DebugType = embedded`.** The embedded PDB
  satisfies debugger needs without putting a `.pdb` in the package, which
  L2-013 #3 forbids by default.

## 3. Repository Layout

```
TokenQ/
  src/TokenQ/                 ← the project that becomes the .nupkg
  tests/TokenQ.Tests/         ← xUnit project, not packed
  README.md                   ← becomes PackageReadmeFile
  docs/specs/                 ← L1.md, L2.md
  docs/detailed-designs/      ← these documents
  .gitignore                  ← excludes ./nupkg, bin, obj
```

A solution file (`TokenQ.sln`) ties the two projects together so
`dotnet build` from the repo root works without arguments.

## 4. Build & Pack Workflow

Three commands cover the local build, package, and smoke-install:

```sh
dotnet build
dotnet pack src/TokenQ/TokenQ.csproj -c Release
dotnet tool install --global --add-source ./src/TokenQ/nupkg TokenQ
tokenq --help
```

The smoke install is the easiest way to validate L2-013 #2 — if the produced
package is malformed, the install fails with a useful error.

## 5. Performance Budget (L2-014)

A single acceptance test in `TokenQ.Tests` (category `Performance`) covers
L2-014:

1. **Warm pipeline (L2-014 #1):** call into the in-process composition root
   100 times in a row, recording the elapsed time of each call. Assert that
   the median is under 100 ms.
2. **Cold start (L2-014 #2):** spawn `dotnet` against the published binary
   in a sub-process with the input `--name IFooService --output <temp>`,
   measure wall-clock. Assert under 2000 ms. Skipped on CI hosts without an
   SSD (detected by reading `IsFixed` and the disk model on Windows; on
   Linux skipped if `$env:CI_NO_SSD == "1"`).
3. **Memory ceiling (L2-014 #3):** run the warm pipeline 100 times in the
   test process, capture `Process.GetCurrentProcess().PeakWorkingSet64`,
   assert under 100 MB. The cold-start timing in #2 makes this likely to
   pass; this is a backstop.

The performance test is gated behind `[Trait("Category","Performance")]` so
it runs only when explicitly requested. Functional ATDD for slices 01-04
runs on every commit; performance runs on tagged releases.

## 6. ATDD Test Plan for This Slice

1. `Csproj_TargetsNet8Only` — parses the project file and asserts the
   `TargetFramework` element value (covers L2-012 #3).
2. `DotnetPack_ProducesPackageWithRequiredMetadata` — runs `dotnet pack`
   into a temp directory, opens the resulting `.nupkg` with
   `System.IO.Packaging`, asserts the manifest contains
   `packAsTool=true`, `toolCommandName=tokenq`, a non-empty version,
   authors, description, and license expression. Covers L2-013 #1.
3. `DotnetPack_PackageContainsOnlyToolsAndMetadata` — opens the same
   `.nupkg` and asserts every entry path either lives under `tools/` or is a
   metadata entry (`.nuspec`, `_rels/`, `[Content_Types].xml`,
   `package/services/metadata/`). Covers L2-013 #3.
4. `Performance_WarmPipeline_Under100ms_Median` — covers L2-014 #1.
5. `Performance_ColdInvocation_Under2s` — covers L2-014 #2 (skipped without
   SSD).
6. `Performance_PeakWorkingSet_Under100MB` — covers L2-014 #3.

The "older .NET host emits clear error" criterion (L2-012 #2) is verified
manually — installing on a host with only .NET 6 produces the .NET host's
own roll-forward error message; we do not own that message. Documented in
the README as a manual-test step before each release.

## 7. Security Considerations

- **License expression.** `MIT` is declared explicitly in the package, so
  consumers know the redistribution terms before installing.
- **Symbol packages.** `IncludeSymbols = false` plus embedded PDB means we
  do not accidentally ship a `.snupkg` to nuget.org with debug detail that
  could expose internal paths from the build agent.
- **Package contents.** L2-013 #3's "no files outside `tools/`" check is a
  guard against accidentally packing a `.config`, key file, or stray asset
  from the project directory.
- **Supply chain.** All `PackageReference`s are first-party Microsoft or
  .NET Foundation packages. There are no third-party dependencies. The
  performance test that runs `dotnet` in a sub-process trusts the developer
  workstation's installed SDK; CI runs in a clean container.

## 8. Open Questions

- **CI/CD.** A GitHub Actions workflow that runs `dotnet test`, then
  `dotnet pack`, then `dotnet nuget push --skip-duplicate` on tag is the
  obvious next step. It is not part of this slice — adding it requires a
  NuGet API key and a tag policy, both deployment concerns rather than
  product concerns.
- **Versioning automation.** `MinVer` or `Nerdbank.GitVersioning` would
  derive `Version` from git tags. Worth adopting only after the first 1.0;
  hand-maintained `<Version>` is sufficient for now.
- **Multi-target.** If a user reports needing a .NET Framework 4.8 build, a
  second `<TargetFramework>` is straightforward. Until then keep the matrix
  at one entry — `dotnet pack` is faster and the CI cost is lower.
- **`dotnet tool` vs `winget`/`brew`.** Out of scope. The tool ships through
  NuGet only.
