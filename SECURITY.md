# Security Policy

## Supported Versions

TokenQ is pre-1.0. Security fixes are applied to the `main` branch until a
formal release line exists.

| Version | Supported |
|---------|-----------|
| `main` | Yes |
| Published pre-1.0 packages | Best effort |

## Reporting a Vulnerability

Do not open a public issue for a vulnerability.

Preferred path:

1. Use GitHub's private vulnerability reporting or Security Advisory feature
   for this repository.
2. If private reporting is not available, open a public issue that asks for a
   private contact path without including vulnerability details.

Include:

- A concise description of the issue.
- Affected version, commit, or package.
- Reproduction steps or proof of concept.
- Expected impact.
- Any known workaround.

## Scope

Security-sensitive areas include:

- Path resolution and file writes.
- Overwrite behavior.
- Generated TypeScript content.
- NuGet package contents and metadata.
- Dependency updates.

TokenQ does not execute generated files, run external commands, or access the
network during generation.

## Disclosure

Maintainers will acknowledge valid reports, investigate, prepare a fix, and
publish release notes once a patched version is available. Public disclosure
should wait until users have a reasonable path to update.
