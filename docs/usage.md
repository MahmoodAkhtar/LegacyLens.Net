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
--artifacts <value>     Optional artifact selection. Accepts one artifact name, a comma-separated list of artifact names, or all.
--class-dependency-type <fully-qualified-type-name>
                        Fully qualified type name for the parameterised class-dependency-scope artifact.
--upgrade-target <tfm>  Optional target-framework context for upgrade-readiness or upgrade-blockers report wording only; does not change discovery scope or perform compatibility checks.
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
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts solution-topology
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts solution-topology,code-complexity,class-dependencies,interface-inventory,data-access
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts all
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts all --upgrade-target net8.0
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts upgrade-readiness --upgrade-target net8.0
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts upgrade-readiness
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts upgrade-blockers --upgrade-target net8.0
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts upgrade-blockers
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts external-dependencies
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts configuration-inventory
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts data-access
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts edmx-analysis
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts class-dependencies
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts class-dependency-scope --class-dependency-type SampleLegacyApp.Services.CustomerService
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts all --class-dependency-type SampleLegacyApp.Services.CustomerService
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts interface-inventory
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts solution-topology
legacylens scan C:\Repos\LegacyApp --output-dir C:\Reports --artifacts code-complexity
legacylens --help
legacylens --version
```

### Visual Progress Feedback

By default, `legacylens scan <path>` should print concise phase-based progress while the scan is running. This is intended to reassure users during large scans without implying a misleading percentage complete value. The progress output should show the current scan phase, completed phase messages, useful counts once known, selected artifact generation progress, elapsed duration, and final output paths.

Normal interactive progress should use a real animated `| / - \` spinner for the currently running phase. The spinner should update the same console line while the phase is active, then stop cleanly, clear or replace the line, and write the completed phase message:

```text
Scanning...

| Discovering projects...   ← same line rotates through | / - \ while active
✓ Projects discovered: 42
| Building file inventory... ← same line rotates through | / - \ while active
✓ Source/config/model files indexed: 1,284
| Scanning WCF configuration...
✓ WCF endpoints discovered: 12
| Writing discovery-report.md...
✓ discovery-report.md generated

Completed in 00:01:34
```

The spinner is current-phase feedback only. It should complement completed phase messages and counts, not replace them, and it should not imply percentage completion. When console output is redirected or non-interactive, animation should be disabled so logs stay readable and deterministic.

When `--quiet` is used, progress messages and spinner output should be suppressed. Quiet output should keep only essential final generated-path output and errors.

When `--verbose` is used, normal phase progress should still be shown, with additional useful diagnostics such as project, file, phase, or artifact details where that helps troubleshoot slow scans. If verbose detail is written while a spinner is active, the spinner line should be stopped or cleared, the detail line should be written cleanly, and the spinner should resume if the phase is still active.

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

### Markdown Report Safety

Generated Markdown reports are intended to be readable in both raw Markdown and rendered previews. Table cells should be formatted through shared Markdown-safe helpers so evidence and discovered values remain visible even when they contain XML-like text, source-code snippets, paths, pipe characters, newlines, or backticks.

For example, Spring.NET XML registration evidence in `interface-inventory.md` should render visibly as inline code:

```markdown
`<object id="customerServiceByInterface" type="SampleLegacyApp.Services.ICustomerService, SampleLegacyApp.Services" factory-object="customerService" factory-method="ToString" />`
```

This formatting must not change discovery behaviour, analyzer models, masking rules, artifact selection, or CLI options. It only affects how generated Markdown table cells are rendered.


### WCF Service-Contract Scan Scope

During normal CLI scanning, WCF service-contract discovery should use the shared project-aware file inventory that is built after project discovery. This keeps the public command contract unchanged, but avoids a second independent recursive scan of every `*.cs` file under the full scan root during the `Scanning WCF service contracts` phase.

The scanner should still detect `[ServiceContract]`, `[ServiceContractAttribute]`, `[OperationContract]`, and `[OperationContractAttribute]` in indexed C# source files, and it should not depend on WCF endpoints or WCF behaviours being discovered first. Source-level service contracts can exist even when configuration endpoints are absent. The inventory-backed normal scan intentionally focuses on C# files associated with discovered project directories and follows the shared file-inventory exclusions for build output and generated output folders.

### Artifact Selection

The main `discovery-report.md` is always generated. The optional `--artifacts <value>` option controls which additional artifact reports are generated from the same scan.

The `class-dependency-scope` artifact is parameterised. It is selected through `--artifacts`, but it also needs `--class-dependency-type <fully-qualified-type-name>` so the scanner knows which root type to centre the scoped report on.

`--artifacts` accepts:

- one artifact name, for example `solution-topology`
- a comma-separated list of artifact names, for example `solution-topology,code-complexity,class-dependencies,data-access`
- the special value `all`, which generates every supported optional artifact

Supported artifact names are:

- `upgrade-readiness`
- `upgrade-blockers`
- `external-dependencies`
- `configuration-inventory`
- `data-access`
- `edmx-analysis`
- `class-dependencies`
- `class-dependency-scope`
- `interface-inventory`
- `solution-topology`
- `code-complexity`
- `all`

Artifact matching is case-insensitive. Comma-separated values may include spaces around commas, so both of these are valid:

```bash
legacylens scan <path> --artifacts solution-topology,code-complexity,class-dependencies
legacylens scan <path> --artifacts solution-topology, code-complexity, class-dependencies, interface-inventory, data-access
```

Duplicate artifact names are ignored so the same report is not generated twice.

`all` must be used by itself. For example, this should be rejected with a clear validation error:

```bash
legacylens scan <path> --artifacts all,data-access
```

Unknown artifact names should produce a clear validation error that lists the supported values.

`--class-dependency-type` should be accepted only when `--artifacts` includes `class-dependency-scope` or uses `all`. If `class-dependency-scope` is explicitly selected without a type name, the CLI should return `The class-dependency-scope artifact requires --class-dependency-type <fully-qualified-type-name>.` If the type option is used with unrelated artifacts, the CLI should return `Use --class-dependency-type only when --artifacts includes class-dependency-scope or all.` `--artifacts all` should remain suitable for normal batch scans and should generate the scoped artifact only when the type option is also supplied.

`--upgrade-target <tfm>` is optional target-framework context for upgrade report wording only. It is valid only when the selected artifacts include `upgrade-readiness`, `upgrade-blockers`, or `all`, and it does not change discovery scope or perform compatibility checks:

```bash
legacylens scan <path> --artifacts upgrade-readiness --upgrade-target net8.0
legacylens scan <path> --artifacts upgrade-blockers --upgrade-target net8.0
legacylens scan <path> --artifacts upgrade-readiness,upgrade-blockers --upgrade-target net8.0
legacylens scan <path> --artifacts all --upgrade-target net8.0
```

`--upgrade-target <tfm>` should be rejected when none of the selected artifacts are upgrade-related:

```bash
legacylens scan <path> --artifacts data-access --upgrade-target net8.0
```

### Upgrade Readiness Artifact

The MVP scope now includes an optional `upgrade-readiness` artifact that should produce:

```text
<output-dir>/upgrade-readiness-report.md
```

Intended usage:

```bash
legacylens scan <path> --output-dir ./output --artifacts upgrade-readiness --upgrade-target net8.0
```

The `--upgrade-target` option is optional wording context only:

```bash
legacylens scan <path> --output-dir ./output --artifacts upgrade-readiness
```

When no upgrade target wording context is provided, the report should still be generated using general upgrade-readiness wording. The report should remain static and evidence-backed. It should not claim to build the solution, run tests, restore NuGet packages, resolve transitive dependencies, inspect NuGet package assets, automatically migrate the codebase, or guarantee compatibility with any destination framework.


### Upgrade Blockers Artifact

The MVP scope now includes an optional `upgrade-blockers` artifact that should produce:

```text
<output-dir>/upgrade-blockers.md
```

Intended usage:

```bash
legacylens scan <path> --output-dir ./output --artifacts upgrade-blockers --upgrade-target net8.0
```

The `--upgrade-target` option is optional wording context only:

```bash
legacylens scan <path> --output-dir ./output --artifacts upgrade-blockers
```

When no upgrade target wording context is provided, the report should still be generated using general upgrade-blocker wording. The report should remain static and evidence-backed. It should identify visible blockers, migration decisions, and higher-risk areas that may complicate upgrade planning, but it should not claim to build the solution, run tests, restore NuGet packages, resolve transitive dependencies, inspect NuGet package assets, automatically migrate the codebase, prove that migration is impossible, or guarantee compatibility with any destination framework.


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



### Configuration Inventory Artifact

The MVP scope now includes an optional `configuration-inventory` artifact that should produce:

```text
<output-dir>/configuration-inventory.md
```

Intended usage:

```bash
legacylens scan <path> --output-dir ./output --artifacts configuration-inventory
```

The report should remain static, evidence-backed, and security-conscious. It should identify visible configuration files, app settings, connection strings, custom sections, environment transforms, WCF configuration, ASP.NET/IIS sections, binding redirects, authentication and authorization settings, logging configuration, Entity Framework configuration, SMTP settings, and configuration API usage where discoverable. It should also map statically discoverable source-code configuration access back to visible configured keys where possible, including literal `ConfigurationManager.AppSettings[...]`, `ConfigurationManager.AppSettings.Get(...)`, and `ConfigurationManager.ConnectionStrings[...]` access patterns. Literal usages should include source file and line evidence; dynamic, computed, interpolated, variable-based, or concatenated keys should be reported as requiring review rather than inventing a key.

The report should not claim to run the application, apply transforms, validate credentials, connect to external systems, prove production usage, prove a setting is used or unused, fully evaluate runtime configuration inheritance, resolve deployment-time substitutions, or guarantee completeness. Keys with no matched static usage should be worded as `No static source usage detected`, not `unused`. Sensitive values such as passwords, API keys, tokens, SAS tokens, storage account keys, client secrets, private feed credentials, and connection string secrets should be masked or redacted.


### Data Access Artifact

The MVP scope now includes an optional `data-access` artifact that should produce:

```text
<output-dir>/data-access-inventory.md
```

Intended usage:

```bash
legacylens scan <path> --output-dir ./output --artifacts data-access
```

The report should remain static, evidence-backed, and security-conscious. It should identify visible data access technologies, patterns, and migration concerns, such as connection strings, database provider indicators, EF6, EDMX/ObjectContext, EF Core, ADO.NET, Dapper, NHibernate, LINQ to SQL, raw SQL indicators, possible stored procedure usage, repository candidates, unit-of-work candidates, and migration artifacts.

The report should not claim to connect to databases, validate credentials, validate connection strings, execute SQL, parse or validate full SQL syntax, inspect live schemas, compare schemas, run EF migrations, scaffold EF Core models, reverse-engineer databases, prove runtime usage, prove a query or stored procedure is unused, automatically migrate data access code, or guarantee EF6-to-EF Core or package compatibility. Sensitive values such as passwords, user names where appropriate, access tokens, API keys, and embedded credentials should be masked or redacted.


### EDMX Analysis Artifact

The MVP scope now includes an optional `edmx-analysis` artifact that should produce:

```text
<output-dir>/edmx-analysis.md
```

Intended usage:

```bash
legacylens scan <path> --output-dir ./output --artifacts edmx-analysis
```

The report should remain static and evidence-backed. It should discover `.edmx` files under scanned projects, associate each EDMX file with the nearest discovered project where possible, parse the EDMX XML safely, and report useful conceptual model, storage model, mapping model, designer metadata, companion generated file, and EF Core migration concern information.

The report should not claim to connect to a database, validate the EDMX against a live database, generate EF Core models, convert EDMX to EF Core, run NuGet restore, build the solution, guarantee migration compatibility, fully understand custom T4 templates, or claim that all EF Core equivalents are direct one-to-one replacements.



### Interface Inventory Artifact

The MVP scope now includes an optional `interface-inventory` artifact that should produce:

```text
<output-dir>/interface-inventory.md
```

Intended usage:

```bash
legacylens scan <path> --output-dir ./output --artifacts interface-inventory
```

The report should remain static and evidence-backed. It should analyse `.cs` source files and visible configuration/XML files where useful to discover source-defined interfaces, implementations, consumers, DI/IoC registration evidence, and dynamic or configuration-driven wiring that requires review. It should help a developer answer which abstractions already exist and where new functionality or replacement behaviour may need to implement, reuse, or review an existing interface. For Spring.NET XML, evidence should come from meaningful configuration-bearing elements and attributes such as `<object id="..." type="...">`, `constructor-arg`, `property`, `factory-object`, `factory-method`, `parent`, `abstract`, alias-style wiring, or similar executable wiring. XML comments, the root `<objects>` element, arbitrary descendant text, and `<description>` text should not create registration evidence or appear in report snippets.

The report should not claim to build the solution, restore packages, execute container bootstrap code, load assemblies, apply transforms, resolve runtime dependency injection, prove runtime usage, prove an interface is unused, prove an interface is registered, or guarantee completeness. Use cautious wording such as `static source evidence`, `static configuration evidence`, `registration evidence found`, `configuration-driven wiring may exist`, `requires review`, `possible extension point`, and `no static source usage detected`. XML/configuration-driven Spring.NET evidence should remain marked as requiring review even when the object or property evidence is meaningful.

### Solution Topology Artifact

The MVP scope now includes an optional `solution-topology` artifact that should produce:

```text
<output-dir>/solution-topology.md
```

Intended usage:

```bash
legacylens scan <path> --output-dir ./output --artifacts solution-topology
```

The report should remain static and evidence-backed. It should help a .NET developer understand solution membership, project relationships, dependency direction, entry points, configuration hotspots, and likely ownership or review boundaries before onboarding, refactoring, or upgrade planning.

The report should not claim to build the solution, restore NuGet packages, execute code, infer runtime call graphs, or prove architectural intent.

### Code Complexity Artifact

The MVP scope now includes an optional `code-complexity` artifact that should produce:

```text
<output-dir>/code-complexity.md
```

Intended usage:

```bash
legacylens scan <path> --output-dir ./output --artifacts code-complexity
```

It can also be selected as part of a comma-separated artifact list or by using `--artifacts all`:

```bash
legacylens scan <path> --output-dir ./output --artifacts solution-topology,code-complexity,class-dependencies
legacylens scan <path> --output-dir ./output --artifacts all
```

The report should estimate cyclomatic complexity from indexed C# source syntax without requiring the solution to build, restore packages, load projects, or create a semantic model. It should report member-level complexity and aggregate by type, namespace, project, and scan root. The report should use cautious wording: complexity values are static discovery signals intended to help prioritise review, testing, simplification, and refactoring work. They are not exact Microsoft, Visual Studio, compiler, runtime-risk, defect-probability, testability, maintainability, or safe-refactoring metrics.

### Class Dependencies Artifact

The MVP scope now includes an optional `class-dependencies` artifact that should produce:

```text
<output-dir>/class-dependencies.md
```

Intended usage:

```bash
legacylens scan <path> --output-dir ./output --artifacts class-dependencies
```

The report should remain static and evidence-backed. It should analyse `.cs` source files under discovered projects and report source-level relationships between types, including constructor parameters, fields, properties, method parameters, return types, local variables, object creation, static member access, inheritance, interface implementations, attributes, and generic type usage.

The report should include coupling summaries, high-dependency hotspots, hardcoded concrete dependencies, static dependency concerns, evidence-backed review notes, a focused Mermaid diagram with dependency-kind edge labels, a full type dependency inventory, and per-type details where useful.

The report should not claim to build the solution, restore NuGet packages, resolve runtime dependency injection, execute code, understand reflection or dynamic loading, fully understand generated code, prove runtime usage, or produce a runtime call graph.

### Scoped Class Dependency Artifact

The MVP scope now includes an optional on-demand `class-dependency-scope` artifact that should produce a type-specific timestamped Markdown file rather than a fixed filename:

```text
<output-dir>/class-dependency-scope.<safe-fully-qualified-type-name>.<yyyyMMdd-HHmmss>.md
```

Intended usage:

```bash
legacylens scan <path> --output-dir ./output --artifacts class-dependency-scope --class-dependency-type SampleLegacyApp.Services.CustomerService
```

Example output filename:

```text
output/class-dependency-scope.SampleLegacyApp.Services.CustomerService.20260620-153045.md
```

Repeated runs for the same type should not overwrite earlier scoped reports. The filename should use local machine time in sortable Windows-safe `yyyyMMdd-HHmmss` form, while the report body should include both local and UTC generated timestamps. The fully qualified type name should be sanitised for use in a filename, preserving letters, numbers, `.`, `_`, and `-` where practical and replacing invalid filename characters with safe separators.

The report should resolve the requested fully qualified type name case-insensitively against discovered source-defined types, show direct outbound dependencies, direct inbound dependants, review concerns involving the root type, and a compact Mermaid diagram centred on that type. If no matching type is found, it should still generate a report with the requested type name, source files analysed, discovered type count, generated timestamps, and a clear no-match message. If multiple discovered types have the same full name, it should report ambiguity with project and source-path evidence rather than guessing.

The scoped report should reuse the existing no-build `ClassDependencyAnalyzer` and shared file inventory instead of introducing a second dependency scanner. It should remain static, evidence-backed, and source-visible only; it should not claim runtime dependency injection resolution, reflection or dynamic loading understanding, transitive dependency completeness, generated-code behaviour, or runtime call graph accuracy.


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
- `--artifacts` accepts one supported artifact name
- `--artifacts` accepts a comma-separated list of supported artifact names
- `--artifacts all` generates every supported optional artifact
- artifact names are matched case-insensitively
- comma-separated artifact names tolerate spaces around commas
- duplicate artifact names are de-duplicated before artifact generation
- unknown artifact names are rejected with a clear validation error listing supported values
- `all` cannot be combined with other artifact names
- `--upgrade-target <tfm>` should be accepted as optional upgrade report wording context only when `upgrade-readiness`, `upgrade-blockers`, or `all` is selected
- `--upgrade-target <tfm>` should be rejected when none of the selected artifacts are upgrade-related because there is no upgrade report wording context to apply
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
