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

### Console Output Modes

Default output prints a concise scan summary, including discovered solution/project counts, dependency counts, WCF counts, legacy ASP.NET artifact counts, configuration file counts, modernisation hint counts, and the top review areas.

As part of the MVP scope, the generated Markdown report should include package compatibility review information for upgrade planning. This enriches package discovery with package version, project target framework, package target framework where available, package source format, and possible compatibility concerns. No separate command is required; the review should be part of the normal `scan` output once implemented.

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
Package reference: Dapper 2.1.66 (source: PackageReference)
Package compatibility concern: Classic Entity Framework should be reviewed before migration to EF Core or modern .NET.
```

Use either `--quiet` or `--verbose`, not both.

### CLI Validation

The CLI currently validates the following:

- a command is required
- the supported command is `scan`
- a scan path is required
- unknown options are rejected
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
