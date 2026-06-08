# Usage

This document contains the command-line usage details for LegacyLens.NET.

## Command Line Usage

LegacyLens.NET is intended to be used as a standalone command-line discovery utility.

The normal usage model is:

```text
download exe
open terminal
run scan
get markdown report
```

Usage:

```text
legacylens scan <path> [options]
```

Arguments:

```text
<path>                 Folder containing one or more .NET solutions or projects.
```

Options:

```text
-o, --output <file>    Markdown report file to create.
--output-dir <dir>     Directory where discovery-report.md should be written.
--format <format>      Report format. Currently only markdown is supported.
--quiet                Only print essential output.
--verbose              Print detailed discovery output.
--artifacts <value>     Optional artifact selection. MVP target includes upgrade-readiness, upgrade-blockers, and external-dependencies.
--upgrade-target <tfm>  Optional requested target framework for upgrade-readiness or upgrade-blockers wording.
-h, --help             Show help.
--version              Show version.
```

Examples:

```bash
legacylens scan .
legacylens scan C:\Repos\LegacyApp
legacylens scan C:\Repos\LegacyApp --output C:\Reports\legacy-discovery.md
legacylens scan C:\Repos\LegacyApp -o C:\Reports\legacy-discovery.md
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports
legacylens scan C:\Repos\LegacyApp --format markdown
legacylens scan C:\Repos\LegacyApp --quiet
legacylens scan C:\Repos\LegacyApp --verbose
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts upgrade-readiness --upgrade-target net8.0
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts upgrade-readiness
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts upgrade-blockers --upgrade-target net8.0
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts upgrade-blockers
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts external-dependencies
legacylens --help
legacylens --version
```

### Report Output

By default, LegacyLens.NET writes the Markdown report to:

```text
<scan-path>/output/discovery-report.md
```

A specific file can be selected with:

```bash
legacylens scan <path> --output <file>
```

or:

```bash
legacylens scan <path> -o <file>
```

A directory can be selected with:

```bash
legacylens scan <path> --output-dir <dir>
```

When `--output-dir` is used, the report file is written as:

```text
<dir>/discovery-report.md
```

Use either `--output` or `--output-dir`, not both.

### Upgrade Readiness Artifact

The MVP scope now includes an optional `upgrade-readiness` artifact that should produce:

```text
<output-dir>/upgrade-readiness-report.md
```

Intended usage:

```bash
legacylens scan <path> --output-dir ./output --artifacts upgrade-readiness --upgrade-target net8.0
```

The `--upgrade-target` option is optional:

```bash
legacylens scan <path> --output-dir ./output --artifacts upgrade-readiness
```

When no upgrade target is provided, the report should still be generated using general upgrade-readiness wording. The report should remain static and evidence-backed. It should not claim to build the solution, run tests, restore NuGet packages, resolve transitive dependencies, inspect NuGet package assets, automatically migrate the codebase, or guarantee compatibility with any destination framework.

For the first implementation, prefer the smallest command support needed to generate `upgrade-readiness-report.md`. Do not over-engineer artifact selection if the existing CLI structure is not ready for a broader artifact system.

### Upgrade Blockers Artifact

The MVP scope now includes an optional `upgrade-blockers` artifact that should produce:

```text
<output-dir>/upgrade-blockers.md
```

Intended usage:

```bash
legacylens scan <path> --output-dir ./output --artifacts upgrade-blockers --upgrade-target net8.0
```

The `--upgrade-target` option is optional:

```bash
legacylens scan <path> --output-dir ./output --artifacts upgrade-blockers
```

When no upgrade target is provided, the report should still be generated using general upgrade-blocker wording. The report should remain static and evidence-backed. It should identify visible blockers, migration decisions, and higher-risk areas that may complicate upgrade planning, but it should not claim to build the solution, run tests, restore NuGet packages, resolve transitive dependencies, inspect NuGet package assets, automatically migrate the codebase, prove that migration is impossible, or guarantee compatibility with any destination framework.

For the first implementation, prefer the smallest command support needed to generate `upgrade-blockers.md`. Do not over-engineer artifact selection if the existing CLI structure is not ready for a broader artifact system.

### External Dependencies Artifact

The MVP scope now includes an optional `external-dependencies` artifact that should produce:

```text
<output-dir>/external-dependencies.md
```

Intended usage:

```bash
legacylens scan <path> --output-dir ./output --artifacts external-dependencies
```

The report should remain static, evidence-backed, and security-conscious. It should identify possible runtime or build-time dependencies outside the repository, such as database connection strings, HTTP/API URLs, WCF endpoints, queues, SMTP/email settings, Redis/cache indicators, file shares, cloud service packages, private NuGet feeds, direct vendor DLL references, and infrastructure-related configuration.

The report should not claim to connect to databases, call HTTP APIs, validate credentials, verify network reachability, inspect production infrastructure, prove production usage, prove that a dependency is unused, or guarantee completeness. Sensitive values such as passwords, API keys, tokens, SAS tokens, access keys, client secrets, private feed credentials, and connection string secrets should be masked or redacted.

For the first implementation, prefer the smallest command support needed to generate `external-dependencies.md`. Do not over-engineer artifact selection if the existing CLI structure is not ready for a broader artifact system.

### Console Output Modes

Default output prints a concise scan summary, including discovered solution/project counts, dependency counts, WCF counts, legacy ASP.NET artifact counts, configuration file counts, modernisation hint counts, and the top review areas.

The generated Markdown report includes package compatibility review information for upgrade planning. This enriches package discovery with package version, project target framework, package target framework where available, package source format, and possible compatibility concerns. No separate command is required; the review is part of the normal `scan` output.

Quiet mode prints only the essential report output:

```bash
legacylens scan <path> --quiet
```

Verbose mode prints detailed discovery output. As package compatibility review is added to the MVP, verbose output should show package versions and package source details where available rather than only package names:

```bash
legacylens scan <path> --verbose
```

Representative package lines:

```text
Package reference: EntityFramework 6.4.4 (source: packages.config, package target framework: net48)
Package reference: Dapper 2.1.35 (source: PackageReference)
Package compatibility concern: Classic Entity Framework should be reviewed before migration to EF Core or modern .NET.
```

Use either `--quiet` or `--verbose`, not both.

### CLI Validation

The CLI currently validates the following:

- a command is required
- the supported command is `scan`
- a scan path is required
- unknown options are rejected
- `--artifacts upgrade-readiness` should be supported when the upgrade-readiness artifact is implemented
- `--artifacts upgrade-blockers` should be supported when the upgrade-blockers artifact is implemented
- `--artifacts external-dependencies` should be supported when the external-dependencies artifact is implemented
- `--upgrade-target <tfm>` should be accepted as optional context for upgrade-readiness and upgrade-blockers wording
- option values are required for `--output`, `-o`, `--output-dir`, and `--format`
- only `markdown` is currently supported for `--format`
- `--output` and `--output-dir` cannot be used together
- `--quiet` and `--verbose` cannot be used together
- a missing scan directory returns a command error

---

## Running the Tool

For normal use, run the published executable:

```bash
legacylens scan <path>
```

Example:

```powershell
PS C:\Repos> legacylens scan .\LegacyApp\
```

This scans the selected folder, prints a concise discovery summary to the console, and generates a Markdown report at the resolved report output path.

By default, the report is written to:

```text
<scan-path>/output/discovery-report.md
```

Example final console lines:

```text
Markdown report generated:
C:\Path\To\LegacyApp\output\discovery-report.md
```

For detailed discovery output, use:

```bash
legacylens scan <path> --verbose
```

For only the essential generated report path, use:

```bash
legacylens scan <path> --quiet
```

## Development Usage

For local development, the CLI project can still be run through `dotnet run` from the repository root:

```powershell
dotnet run --project src/LegacyLens.Cli -- scan .\samples\SampleLegacyApp\
```

This development workflow exercises the same `scan` command contract as the published executable.

---
