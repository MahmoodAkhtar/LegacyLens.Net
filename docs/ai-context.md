# LegacyLens.NET AI Context

This file is intended to be used as concise context for AI-assisted development chats.

## Product purpose

LegacyLens.NET is a standalone static discovery CLI for unfamiliar, legacy, and modern .NET codebases.

It scans source and configuration files to produce Markdown reports that help a developer understand structure, dependencies, package compatibility review signals, upgrade-readiness signals, upgrade-blocker decision signals, possible external dependency signals, data access inventory signals, EDMX analysis signals, class dependency analysis signals, legacy technology indicators, configuration concerns, and prioritised modernisation review areas.

## Usage model

```text
download exe
open terminal
run scan
get markdown report
```

## Current command contract

```text
legacylens scan <path> [options]
```

Important options:

```text
-o, --output <file>    Markdown report file to create.
--output-dir <dir>     Directory where discovery-report.md should be written.
--format <format>      Report format. Currently only markdown is supported.
--quiet                Only print essential output.
--verbose              Print detailed discovery output.
--artifacts <value>     Optional artifact selection. Accepts one artifact name, a comma-separated list of artifact names, or all.
--upgrade-target <tfm>  Optional target-framework context for upgrade-readiness or upgrade-blockers report wording only; does not change discovery scope or perform compatibility checks.
-h, --help             Show help.
--version              Show version.
```

## Visual progress feedback

Visual progress feedback is MVP scope as a CLI console UX capability. It should make long scans feel active and trustworthy by showing phase-based progress rather than a misleading percentage progress bar. Normal mode should show concise current-phase messages, completed phase messages, useful counts once known, elapsed duration, selected artifact generation progress, and final generated output paths. A simple `| / - \` spinner or spinner-like prefix may be used for the currently running phase, but it should complement completed phase messages and counts rather than replace them.

`--quiet` should suppress non-essential progress and spinner output, while keeping essential final generated-path output and errors. `--verbose` should include normal phase progress plus useful per-project, per-file, per-phase, or per-artifact details for troubleshooting slow scans. Keep progress reporting in `LegacyLens.Cli`, preferably behind an abstraction such as `IScanProgressReporter`, and do not put console output in `LegacyLens.Core`. This capability must not change discovery semantics, artifact selection semantics, or generated Markdown report contents.

## Current output

By default, the report is generated at:

```text
<scan-path>/output/discovery-report.md
```

The main discovery report currently includes solution, project, target framework, package reference, assembly reference, project reference, WCF, Legacy ASP.NET, configuration, modernisation hint, modernisation review summary, and Mermaid dependency diagram sections. The MVP scope also includes package compatibility review, a separate `upgrade-readiness-report.md` artifact, a separate `upgrade-blockers.md` artifact, a separate `external-dependencies.md` artifact for identifying possible external runtime and build-time dependencies, a separate `configuration-inventory.md` artifact for identifying visible configuration files, settings, sections, transforms, and configuration API usage, a separate `data-access-inventory.md` artifact for identifying visible data access technologies, patterns, and migration concerns, a separate `edmx-analysis.md` artifact for inspecting EF EDMX model contents and EF Core migration concern signals, and a separate `class-dependencies.md` artifact for source-level type relationship and coupling analysis, and a separate `solution-topology.md` artifact for solution/project relationship orientation.

## Current implemented capability summary

LegacyLens.NET currently discovers:

- `.sln` and `.csproj` structure
- target frameworks
- project references
- assembly references
- NuGet package references from `<PackageReference />` and `packages.config`
- WCF endpoints, binding details, reader quotas, behaviours, service contracts, and operation contracts
- legacy ASP.NET file artifacts such as WebForms pages, user controls, master pages, ASMX services, handlers, and `Global.asax`
- ASP.NET MVC and Web API controllers, actions, route attributes, action attributes, startup registration, route configuration, bundle/filter configuration, dependency resolver setup, controller factory setup, model binder/value provider setup, formatter/message handler/filter/CORS configuration
- `app.config` and `web.config` configuration details
- evidence-backed modernisation hints
- prioritised modernisation review areas
- external dependency inventory inputs such as connection strings, URL-like settings, WCF endpoints, infrastructure packages, private package feed evidence, direct assembly/vendor DLL references, and path/share indicators where discoverable
- data access inventory inputs such as connection strings, provider names, EF6, EF Core, EDMX/T4, LINQ to SQL, ADO.NET, Dapper, NHibernate, raw SQL, stored procedure, repository, unit-of-work, and migration artifact indicators where discoverable
- EDMX analysis inputs such as `.edmx` files, CSDL conceptual entities/entity sets/keys/navigation properties/complex types/function imports, SSDL storage entity sets/tables/views/columns/functions/defining queries, MSL mappings/scalar properties/function import mappings/modification function mappings/query views, designer metadata, and companion generated files where discoverable
- class dependency analysis inputs such as source-defined types, constructor parameters, fields, properties, method parameters, return types, local variables, object creation, static member access, base classes, interface implementations, attributes, generic type usage, coupling hotspots, hardcoded concrete dependencies, and static dependency concerns where discoverable


## MVP artifact selection addition

`--artifacts` is MVP scope as a flexible optional artifact selection capability. It supports one artifact name, a comma-separated list of artifact names, or the special value `all`.

Examples:

```bash
legacylens scan <path> --artifacts solution-topology
legacylens scan <path> --artifacts solution-topology,class-dependencies,data-access
legacylens scan <path> --artifacts all
```

The normal `discovery-report.md` is always generated. Artifact names are case-insensitive. Comma-separated values may contain spaces around commas. Duplicate artifact names should be de-duplicated so reports are not generated twice. Unknown artifact names should produce a clear validation error listing the supported values. `all` must not be combined with other artifact names.

Supported artifact names are `upgrade-readiness`, `upgrade-blockers`, `external-dependencies`, `configuration-inventory`, `data-access`, `edmx-analysis`, `class-dependencies`, and `solution-topology`.

`--upgrade-target <tfm>` is optional target-framework context for upgrade report wording only. It is valid only when selected artifacts include `upgrade-readiness`, `upgrade-blockers`, or `all`, and it should be rejected when none of the selected artifacts are upgrade-related. It must not change discovery scope, enable extra analysis, or imply compatibility checking.

Implementation should keep the artifact runner model. Prefer a parsed artifact selection model on `ScanOptions`, such as selected artifact names, `ShouldWriteAllArtifacts`, and `ShouldWriteArtifact(string artifactName)`. Avoid returning to repeated artifact `if` blocks inside `ScanCommand`.

## MVP package compatibility review addition

Package compatibility review is now MVP scope. It should enrich package discovery with package version, project target framework, package target framework where available, source format, source path, and possible compatibility concerns. It should remain static and evidence-backed, and it should not be described as full NuGet restore, transitive dependency resolution, online package lookup, package asset inspection, or guaranteed compatibility checking against a destination framework.

## MVP upgrade-readiness addition

`upgrade-readiness` is now an MVP-scope capability. It should produce `upgrade-readiness-report.md` as a separate Markdown artifact. It is a static, evidence-backed upgrade readiness assessment for .NET legacy codebases.

It should show current project target frameworks, project-level upgrade candidates, possible upgrade concerns, package and assembly reference considerations, and configuration/runtime risks such as `System.Web`, WCF, EF6, `packages.config`, direct assembly references, `Web.config`, or legacy ASP.NET artifacts.

Intended command shape:

```bash
legacylens scan <path> --output-dir ./output --artifacts upgrade-readiness --upgrade-target net8.0
```

`--upgrade-target` is optional wording context only. If omitted, use general upgrade-readiness wording.

The capability must not claim to build the solution, run tests, restore packages, resolve transitive dependencies, inspect NuGet package assets, automatically migrate code, or guarantee compatibility with any destination framework. Use cautious wording such as `Possible concern`, `Requires review`, `Evidence found`, `May need migration work`, and `Likely upgrade consideration`.

## MVP upgrade-blockers addition

`upgrade-blockers` is now an MVP-scope capability. It should produce `upgrade-blockers.md` as a separate Markdown artifact. It is a static, evidence-backed blocker and decision report for .NET upgrade planning.

Where `upgrade-readiness` answers how ready the solution looks for upgrade, `upgrade-blockers` answers what visible blockers and decisions could stop or complicate the upgrade. It should group findings such as `System.Web`, legacy ASP.NET artifacts, WCF/System.ServiceModel, EF6/EDMX/data-access indicators, `packages.config`, direct assembly or local DLL references, configuration/runtime coupling, Windows-only/platform-specific APIs, and custom build/MSBuild behaviour into clear blocker categories.

Intended command shape:

```bash
legacylens scan <path> --output-dir ./output --artifacts upgrade-blockers --upgrade-target net8.0
```

`--upgrade-target` is optional wording context only. If omitted, use general upgrade-blocker wording.

The report should include a summary, target, blocker overview, upgrade blockers and decisions table, blocker details, evidence, why each blocker matters, decisions required, suggested review order, and notes/limitations. It must not claim to build the solution, run tests, restore packages, resolve transitive dependencies, inspect NuGet package assets, automatically migrate code, prove migration is impossible, or guarantee compatibility with any destination framework. Use cautious wording such as `Possible blocker`, `Potential blocker`, `Requires review`, `Migration decision required`, `Evidence found`, `May complicate upgrade`, and `May require replacement or redesign`.


## MVP external-dependencies addition

`external-dependencies` is now an MVP-scope capability. It should produce `external-dependencies.md` as a separate Markdown artifact. It is a static, evidence-backed inventory of possible external runtime and build-time dependencies used by a .NET codebase.

It should help identify systems, services, infrastructure, files, databases, queues, APIs, package feeds, vendor assemblies, or operational resources outside the repository that may need confirmation before migration, testing, deployment, onboarding, CI setup, or local development.

Intended command shape:

```bash
legacylens scan <path> --output-dir ./output --artifacts external-dependencies
```

The report should group findings such as connection strings, HTTP/API URLs, WCF endpoints, queue-related settings or packages, SMTP/email settings, Redis/cache indicators, file shares, cloud service packages, private NuGet feeds, direct vendor DLL references, and infrastructure-related configuration.

The report should include a summary, analysis scope, dependency overview, dependency table, category-specific sections, suggested questions to ask the team, and notes/limitations. It must not claim to connect to external systems, validate credentials, verify reachability, inspect production infrastructure, prove production usage, prove that a dependency is unused, expose secrets, or guarantee completeness. Sensitive values should be masked or redacted. Use cautious wording such as `Possible external dependency`, `Evidence found`, `Requires confirmation`, `Configured dependency`, `May indicate dependency`, and `Potential runtime dependency`.


## MVP configuration-inventory addition

`configuration-inventory` is now an MVP-scope capability. It should produce `configuration-inventory.md` as a separate Markdown artifact. It is a static, evidence-backed inventory of visible configuration files, configuration values, configuration sections, transforms, and migration-relevant configuration concerns in a .NET codebase.

Intended command shape:

```bash
legacylens scan <path> --output-dir ./output --artifacts configuration-inventory
```

The report should identify visible configuration files, app settings, connection strings, custom sections, environment transforms, WCF configuration, ASP.NET/IIS sections, binding redirects, authentication and authorization settings, logging configuration, Entity Framework configuration, SMTP settings, JSON configuration values, and configuration API usage where discoverable.

Detailed configuration findings should be grouped by project and source file, then by category within each file. This makes it easier for a .NET developer to find which file contains a setting. Per-file detail tables should use compact columns such as `Name`, `Value`, `Evidence`, and `Requires Review`, rather than repeating `Category` and `Source File` on every row.

Value semantics:

- `Value` means the discovered value with sensitive parts masked where needed.
- Use `N/A` for structural findings with no scalar value, such as file presence, section presence, binding redirects, transforms, `.settings` files, and `NuGet.config` file-level findings.
- Do not render missing scalar values as `Unknown` unless the analyzer genuinely tried and failed to determine a value.
- Preserve useful non-secret parts of connection-string-like values while masking embedded credentials.
- For AMQP/RabbitMQ URI values, prefer output such as `amqp://***:***@rabbitmq-dev:5672/sample` rather than full redaction where possible.

The capability must not claim to run the application, apply transforms, validate credentials, connect to external systems, prove production usage, prove a setting is used or unused, fully evaluate runtime configuration inheritance, resolve deployment-time substitutions, expose full secrets, or guarantee completeness. Sensitive values should be masked or redacted. Use cautious wording such as `Configuration evidence found`, `Possible runtime configuration`, `Requires review`, `May need migration`, `Configured setting`, and `Potential migration concern`.

Security expectations:

- Do not print full secrets into Markdown reports.
- Mask or redact passwords, API keys, tokens, client secrets, connection string passwords, URI credentials, SAS tokens, storage account keys, certificate/private-key material, private feed credentials, and usernames inside sensitive values where appropriate.
- For connection strings, prefer reporting name, source file, provider name where available, safe value, embedded credential indicator where useful, redaction status, and possible migration concern rather than the full raw value.

## MVP data-access addition

`data-access` is now an MVP-scope capability. It should produce `data-access-inventory.md` as a separate Markdown artifact. It is a static, evidence-backed inventory of visible data access technologies, patterns, and migration concerns in a .NET codebase.

It should help identify how the application appears to access databases and persistence infrastructure before migration or refactoring work starts. It should group findings such as connection strings, database provider indicators, `EntityFramework`, `Microsoft.EntityFrameworkCore`, EDMX/ObjectContext, EF-related T4 templates, LINQ to SQL `.dbml` files, ADO.NET usage, Dapper, NHibernate, raw SQL indicators, possible stored procedure usage, repository candidates, unit-of-work candidates, and migration artifacts.

Intended command shape:

```bash
legacylens scan <path> --output-dir ./output --artifacts data-access
```

The report should include a summary, analysis scope, data access overview, projects with data access indicators, connection strings, ORM and data access technologies, EF/EDMX details, DbContext/ObjectContext candidates, repository and unit-of-work candidates, raw SQL and stored procedure indicators, database provider indicators, suggested files to review first, migration considerations, suggested questions to ask the team, and notes/limitations.

The capability must not claim to connect to databases, validate credentials or connection strings, execute SQL, parse or validate full SQL syntax, inspect or compare live schemas, run EF migrations, scaffold EF Core models, reverse-engineer databases, prove runtime usage, prove unused queries or stored procedures, automatically migrate data access code, or guarantee EF6-to-EF Core or package compatibility. Sensitive values should be masked or redacted. Use cautious wording such as `Evidence found`, `Possible data access dependency`, `Requires review`, `May indicate database usage`, `Possible stored procedure usage`, `Migration consideration`, and `Should be verified by the development team`.

## MVP edmx-analysis addition

`edmx-analysis` is now an MVP-scope capability. It should produce `edmx-analysis.md` as a separate Markdown artifact. It is a static, evidence-backed EDMX inspection report for legacy EF6 Database First or Model First projects.

It should help identify which projects contain `.edmx` files, what conceptual entities and entity sets are defined, what storage tables/views/functions are represented, what mappings exist between conceptual and storage models, whether associations, navigation properties, complex types, function imports, stored procedure mappings, query views, defining queries, designer metadata, or companion generated files are present, and what EF Core migration concerns are visible from static evidence.

Intended command shape:

```bash
legacylens scan <path> --output-dir ./output --artifacts edmx-analysis
```

The report should include a summary, EDMX files table, upgrade concerns, conceptual model details, storage model details, associations, function imports and store functions, mapping details, companion generated files, and notes/limitations. If no EDMX files are discovered, the report should still be valid and clearly state that no EDMX files were discovered.

The capability must not claim to connect to a database, validate the EDMX against a live database or schema, generate EF Core models, convert EDMX to EF Core, run NuGet restore, build the solution, guarantee migration compatibility, fully understand custom T4 templates, or claim that all EF Core equivalents are direct one-to-one replacements. Use cautious wording such as `Evidence found`, `Requires review`, `Possible migration concern`, `May require manual mapping`, `Should be verified by the development team`, and `No direct EF Core EDMX equivalent`.


## MVP class-dependencies addition

`class-dependencies` is now an MVP-scope capability. It should produce `class-dependencies.md` as a separate Markdown artifact. It is a static, evidence-backed source-level dependency report for understanding class and type coupling inside a .NET solution.

It should help a developer answer: “Inside these projects, which classes are actually coupled to which other classes?” This is useful because project-level references do not show class entanglement, hardcoded concrete construction, static access, inheritance coupling, or high-dependency hotspots.

Intended command shape:

```bash
legacylens scan <path> --output-dir ./output --artifacts class-dependencies
```

The capability should analyse `.cs` source files under discovered projects and identify source-level relationships from constructor parameters, fields, properties, method parameters, return types, local variables, object creation, static member access, base classes, interface implementations, attributes, and generic type arguments.

The report should include a summary, top coupled types, coupling concerns, hardcoded concrete dependencies, static dependency hotspots, a focused Mermaid dependency diagram with dependency-kind edge labels, a full type dependency inventory, type details, and notes/limitations.

Concern severity should use `High`, `Medium`, and `Low`. High findings include hardcoded concrete object creation, direct construction of infrastructure dependencies such as `SmtpClient`, `HttpClient`, or `SqlConnection`, and static access to legacy/global infrastructure where it affects upgrade or testability. Medium findings include static helper/configuration access, concrete field or property dependencies, constructor parameters to concrete classes, inheritance from concrete base classes, framework-specific attributes, and time access such as `DateTime.Now` or `DateTime.UtcNow`. Low findings include constructor parameters to interfaces, interface implementations, normal domain/model method parameters or return types, generic type usage, and informational attributes.

The capability must not claim to build the solution, restore NuGet packages, resolve runtime dependency injection, execute code, understand reflection or dynamic loading, fully understand generated code, prove runtime usage, prove unused dependencies, or produce runtime call graphs. Use cautious wording such as `source-level dependency`, `possible coupling concern`, `suggested review`, `evidence`, and `static analysis finding`.

## Design constraints

- Static-first discovery.
- The target solution does not need to build.
- Report findings should be evidence-backed where possible.
- Discovery signals should be honest and not pretend to be full migration advice.
- MVP usefulness is based on report quality, not exhaustive semantic analysis.
- Avoid duplicated, misleading, or materially low-value report findings.

## MVP definition

The MVP is complete when LegacyLens.NET can statically scan a representative legacy .NET solution and produce readable Markdown reports that help a developer identify the main structure, dependencies, package compatibility review signals, upgrade-readiness signals, upgrade-blocker decision signals, possible external dependency signals, data access inventory signals, EDMX analysis signals, class dependency analysis signals, legacy technology indicators, configuration concerns, and prioritised modernisation review areas.

Further work should be treated as post-MVP unless it fixes a specific report-quality defect.

Package compatibility review is now MVP scope, but it should remain static and evidence-backed. It should not be described as full NuGet restore, transitive dependency resolution, online package lookup, package asset inspection, or guaranteed compatibility checking against a destination framework.

Upgrade blockers is now MVP scope, but it should remain static, evidence-backed, and decision-oriented. It should not be described as a definitive compatibility checker, automatic migration tool, or proof that migration is impossible.

External dependencies is now MVP scope, but it should remain static, evidence-backed, and security-conscious. It should not be described as a runtime dependency mapper, network scanner, credential validator, production dependency list, or complete dependency map.

Data access is now MVP scope, but it should remain static, evidence-backed, and security-conscious. It should not be described as a database schema discovery tool, SQL validator, ORM migration tool, live runtime dependency mapper, credential validator, or guaranteed data-access migration plan.

EDMX analysis is now MVP scope, but it should remain static, evidence-backed, and no-build. It should not be described as an EDMX-to-EF Core converter, EF Core model generator, live database validator, schema comparer, custom T4 semantic analyser, or guaranteed EDMX migration plan.

## Recommended AI prompt context bundle

For implementation chats, provide:

```text
docs/ai-context.md
output/merged_output.txt
output/discovery-report.md
```

Include `README.md` only when the task is specifically about public documentation alignment, release readiness, or README drift.

## Documentation map

- `README.md` is the public front door.
- `docs/usage.md` contains command usage.
- `docs/report-output.md` contains console, discovery report, upgrade-readiness report, upgrade-blockers report, external-dependencies report, and data-access inventory examples.
- `docs/discovery-capabilities.md` contains detailed scanner capability information.
- `docs/architecture.md` contains repository and project structure.
- `docs/mvp.md` defines MVP scope and exit criteria.
- `docs/roadmap.md` captures post-MVP direction.
