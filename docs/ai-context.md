# LegacyLens.NET AI Context

This file is intended to be used as concise context for AI-assisted development chats.

## Product purpose

LegacyLens.NET is a standalone static discovery CLI for unfamiliar, legacy, and modern .NET codebases.

It scans source and configuration files to produce Markdown reports that help a developer understand structure, dependencies, package compatibility review signals, upgrade-readiness signals, upgrade-blocker decision signals, possible external dependency signals, data access inventory signals, EDMX analysis signals, class dependency analysis signals, code complexity review signals, legacy technology indicators, configuration concerns, and prioritised modernisation review areas.

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
--class-dependency-type <fully-qualified-type-name>
                        Fully qualified type name for the parameterised class-dependency-scope artifact.
--class-refactoring-type <fully-qualified-type-name>
                        Fully qualified type name for the parameterised class-refactoring-opportunities artifact.
--upgrade-target <tfm>  Optional target-framework context for upgrade-readiness or upgrade-blockers report wording only; does not change discovery scope or perform compatibility checks.
-h, --help             Show help.
--version              Show version.
```

## Visual progress feedback

Visual progress feedback is MVP scope as a CLI console UX capability. It should make long scans feel active and trustworthy by showing phase-based progress rather than a misleading percentage progress bar. Normal interactive mode should show concise current-phase messages using a real animated `| / - \` spinner that updates the same console line while the phase is active, then stops cleanly and writes completed phase messages with useful counts once known. It should also show elapsed duration, selected artifact generation progress, and final generated output paths. The spinner complements completed phase messages and counts rather than replacing them.

`--quiet` should suppress non-essential progress and spinner output, while keeping essential final generated-path output and errors. Redirected or non-interactive output should avoid live carriage-return animation and remain deterministic line-based text. `--verbose` should include normal phase progress plus useful per-project, per-file, per-phase, or per-artifact details for troubleshooting slow scans; verbose lines should pause or clear the active spinner and then resume it if the phase is still active. Keep progress reporting in `LegacyLens.Cli`, preferably behind an abstraction such as `IScanProgressReporter` with scoped phase operations, and do not put console output in `LegacyLens.Core`. This capability must not change discovery semantics, artifact selection semantics, or generated Markdown report contents.

## Current output

By default, the report is generated at:

```text
<scan-path>/output/discovery-report.md
```

The main discovery report currently includes solution, project, target framework, package reference, assembly reference, project reference, WCF, Legacy ASP.NET, configuration, modernisation hint, modernisation review summary, and Mermaid dependency diagram sections. The MVP scope also includes package compatibility review, a separate `upgrade-readiness-report.md` artifact, a separate `upgrade-blockers.md` artifact, a separate `external-dependencies.md` artifact for identifying possible external runtime and build-time dependencies, a separate `configuration-inventory.md` artifact for identifying visible configuration files, settings, sections, transforms, configuration API usage, source-code configuration key usage, and cautious reconciliation against visible configured entries, a separate `data-access-inventory.md` artifact for identifying visible data access technologies, patterns, and migration concerns, a separate `edmx-analysis.md` artifact for inspecting EF EDMX model contents and EF Core migration concern signals, and a separate `class-dependencies.md` artifact for source-level type relationship and coupling analysis, a parameterised timestamped `class-dependency-scope.<type>.<timestamp>.md` artifact for focused per-type dependency review, a parameterised timestamped `class-refactoring-opportunities.<type>.<timestamp>.md` artifact for focused per-class refactoring approach planning, a separate `interface-inventory.md` artifact for source-level interface, implementation, consumer, registration, and extension-point analysis, a separate `solution-topology.md` artifact for solution/project relationship orientation, and a separate `code-complexity.md` artifact for static no-build cyclomatic complexity estimates and refactoring hotspot review.

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
- configuration-inventory source usage inputs such as literal `ConfigurationManager.AppSettings[...]`, `ConfigurationManager.AppSettings.Get(...)`, and `ConfigurationManager.ConnectionStrings[...]` access patterns where discoverable, plus dynamic configuration key access that requires review
- evidence-backed modernisation hints
- prioritised modernisation review areas
- external dependency inventory inputs such as connection strings, URL-like settings, WCF endpoints, infrastructure packages, private package feed evidence, direct assembly/vendor DLL references, and path/share indicators where discoverable
- data access inventory inputs such as connection strings, provider names, EF6, EF Core, EDMX/T4, LINQ to SQL, ADO.NET, Dapper, NHibernate, raw SQL, stored procedure, repository, unit-of-work, and migration artifact indicators where discoverable
- EDMX analysis inputs such as `.edmx` files, CSDL conceptual entities/entity sets/keys/navigation properties/complex types/function imports, SSDL storage entity sets/tables/views/columns/functions/defining queries, MSL mappings/scalar properties/function import mappings/modification function mappings/query views, designer metadata, and companion generated files where discoverable
- class dependency analysis inputs such as source-defined types, constructor parameters, fields, properties, method parameters, return types, local variables, object creation, static member access, base classes, interface implementations, attributes, generic type usage, coupling hotspots, hardcoded concrete dependencies, and static dependency concerns where discoverable
- scoped class dependency analysis output for one requested fully qualified type, including direct outbound dependencies, direct inbound dependants, related concerns, and a compact Mermaid diagram where source-level evidence allows it
- scoped class refactoring opportunities output for one requested fully qualified type, including evidence-backed testability barriers, existing seams, missing or weak seams, characterization-test targets, relevant Working Effectively with Legacy Code-inspired technique recommendations, and a cautious low-risk/high-value order of approach
- code complexity analysis inputs for `code-complexity.md`, using Roslyn syntax parsing over indexed project-associated C# files to estimate cyclomatic complexity at member level and aggregate it by type, namespace, project, and scan root, including likely generated-code indicators where cheaply detectable
- interface inventory analysis inputs such as interface definitions, implementations, consumers, generic and collection-based interface usage, service-locator usage, Microsoft DI registrations, legacy IoC registration patterns, and Spring.NET/Castle Windsor/Unity-style XML or configuration-driven wiring where discoverable, with Spring.NET XML comments, descriptions, root container text, and arbitrary descendant text ignored as registration evidence



## WCF service-contract scanner performance refactor

The WCF service-contract scanner performance refactor is MVP scope. `WcfServiceContractScanner` should keep its name because it still collects raw WCF source-contract evidence. During normal CLI scans, `ScanCommand` should call an inventory-based overload, such as `Scan(ScanFileInventory fileInventory)` or `Scan(IReadOnlyCollection<ScanFile> csharpFiles)`, using the shared project-aware file inventory already built after project discovery.

The normal CLI path must not perform a second recursive `Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories)` over the full scan root. It should iterate indexed C# files, use `ScanFile.Content` where available, and cheaply skip files that do not contain `ServiceContract` or `ServiceContractAttribute` before applying the existing service-contract/interface parsing logic. Detection should still support `[ServiceContract]`, `[ServiceContractAttribute]`, `[OperationContract]`, and `[OperationContractAttribute]`, and it must not be conditional on WCF endpoints or WCF behaviours being found.

The existing `Scan(string rootPath)` overload can remain for compatibility and existing tests, preserving null/empty and missing-path exception behaviour, but it should delegate into shared parsing logic where practical. The inventory-backed normal scan intentionally focuses on C# files associated with discovered project directories and should respect shared inventory exclusions such as `bin`, `obj`, `output`, `reports`, and other generated/build-output folders. This is a performance and architecture alignment change; it must not change the public CLI contract, artifact selection behaviour, static/no-build model, or generated Markdown report format.

## Markdown report rendering safety

Markdown-safe table-cell formatting is MVP scope as a shared reporting-layer concern. All generated Markdown reports and optional artifacts should keep table rows structurally valid and keep evidence visible in raw Markdown and rendered previews such as VS Code Markdown preview. Writers should avoid writing raw evidence, XML, configuration snippets, source-code snippets, paths, names, or other discovered values directly into table cells when those values may contain Markdown-sensitive characters.

Prefer a shared helper in `LegacyLens.Reporting.Markdown`, for example `MarkdownTableCell.Escape(...)`, `MarkdownTableCell.Code(...)`, and `MarkdownTableCell.Evidence(...)`, or an equivalent existing helper. Evidence cells should be treated as code-like by default. XML-like evidence such as `<object ... />`, `<endpoint ... />`, and `<add ... />` should render visibly as safe inline code rather than being interpreted as raw HTML/XML. Values containing `|`, newlines, or backticks should not split rows or break inline-code formatting.

This must not change discovery behaviour, analyzer models, raw evidence values, masking rules, artifact selection, or CLI output semantics. Keep the change in Markdown writers/helpers and cover it with report-writer tests, including Spring.NET XML registration evidence in `interface-inventory.md`, review findings that reuse XML evidence, and at least one non-interface artifact writer to prove the helper is shared.


## Class refactoring opportunities MVP addition

`class-refactoring-opportunities` is MVP scope as an optional parameterised artifact. Intended usage:

```bash
legacylens scan <path> --output-dir ./output --artifacts class-refactoring-opportunities --class-refactoring-type SampleLegacyApp.Services.CustomerService
legacylens scan <path> --output-dir ./output --artifacts all --class-refactoring-type SampleLegacyApp.Services.CustomerService
```

Validation rules: explicit `class-refactoring-opportunities` selection requires `--class-refactoring-type <fully-qualified-type-name>`; `--class-refactoring-type` is valid only with `class-refactoring-opportunities` or `all`; plain `--artifacts all` must not require the type and should generate this scoped artifact only when the type option is supplied. The existing `--class-dependency-type` option remains for `class-dependency-scope`.

Generated files should be named `class-refactoring-opportunities.<safe-fully-qualified-type-name>.<yyyyMMdd-HHmmss>.md` using local sortable timestamp format, with both local and UTC generated timestamps in the report body. Repeated runs should preserve historical reports.

The analyzer should consume `ScanContext.FileInventory.CSharpFiles` or an equivalent indexed C# file collection. It must not perform its own recursive `*.cs` filesystem walk. Reuse existing class dependency, scoped class dependency, code complexity, interface inventory, configuration inventory, external dependency, and data-access outputs where practical. Artifact-specific Roslyn syntax inspection is acceptable only over the shared inventory and must remain static/no-build.

The recommended design is a pipeline: class source and existing reports, class refactoring profile, evidence-backed signals, recommendation rules, suggested approach, Markdown report. Do not create one analyzer per Working Effectively with Legacy Code technique. The report should be discriminating and evidence-backed; when evidence is weak, say `Not enough evidence` or `No strong recommendation`.

The report should help answer: what makes this class hard to change, where are the seams or missing seams, what should be tested first, whether dependency breaking is needed before direct characterization, which techniques fit the evidence, and what low-risk/high-value order of work should come before refactoring. It must not refactor code, generate patches, build the solution, run tests, execute code, resolve runtime DI, prove runtime call graphs, prove dependencies are unused, or claim that refactoring is safe.

## MVP artifact selection addition

`--artifacts` is MVP scope as a flexible optional artifact selection capability. It supports one artifact name, a comma-separated list of artifact names, or the special value `all`.

Examples:

```bash
legacylens scan <path> --artifacts solution-topology
legacylens scan <path> --artifacts solution-topology,code-complexity,class-dependencies,interface-inventory,data-access
legacylens scan <path> --artifacts all
```

The normal `discovery-report.md` is always generated. Artifact names are case-insensitive. Comma-separated values may contain spaces around commas. Duplicate artifact names should be de-duplicated so reports are not generated twice. Unknown artifact names should produce a clear validation error listing the supported values. `all` must not be combined with other artifact names.

Supported artifact names are `upgrade-readiness`, `upgrade-blockers`, `external-dependencies`, `configuration-inventory`, `data-access`, `edmx-analysis`, `class-dependencies`, `class-dependency-scope`, `class-refactoring-opportunities`, `interface-inventory`, `solution-topology`, and `code-complexity`.

`class-dependency-scope` is a parameterised artifact. It requires `--class-dependency-type <fully-qualified-type-name>` when explicitly selected, is valid with `--artifacts all` only as an additional scoped output when the type option is supplied, and must not make plain `--artifacts all` require a type name. `class-refactoring-opportunities` follows the same parameterised pattern with `--class-refactoring-type <fully-qualified-type-name>` and must not make plain `--artifacts all` require a type name.

`--upgrade-target <tfm>` is optional target-framework context for upgrade report wording only. It is valid only when selected artifacts include `upgrade-readiness`, `upgrade-blockers`, or `all`, and it should be rejected when none of the selected artifacts are upgrade-related. It must not change discovery scope, enable extra analysis, or imply compatibility checking.

Implementation should keep the artifact runner model. Prefer a parsed artifact selection model on `ScanOptions`, such as selected artifact names, `ShouldWriteAllArtifacts`, and `ShouldWriteArtifact(string artifactName)`. Avoid returning to repeated artifact `if` blocks inside `ScanCommand`.

## MVP package compatibility review addition
## MVP scoped class dependency artifact addition

`class-dependency-scope` is MVP scope as an on-demand parameterised artifact for focused refactoring and codebase navigation. Intended usage:

```bash
legacylens scan <path> --output-dir ./output --artifacts class-dependency-scope --class-dependency-type SampleLegacyApp.Services.CustomerService
```

The generated file should use a type-specific, timestamped, Windows-safe filename such as:

```text
class-dependency-scope.SampleLegacyApp.Services.CustomerService.20260620-153045.md
```

The filename timestamp should be local machine time in sortable `yyyyMMdd-HHmmss` form. The report body should include both local and UTC generated timestamps. Repeated runs should preserve historical reports by default.

Implementation should reuse `ClassDependencyAnalyzer` and `ScanContext.FileInventory`; do not create a second dependency scanner. Prefer a scoped projection over the existing `ClassDependencyReport`, with focused models/writers such as `ScopedClassDependencyReport`, `ScopedClassDependencyAnalyzer`, `ScopedClassDependencyMarkdownReportWriter`, `ScopedClassDependencyMermaidDiagramWriter`, and `ScopedClassDependencyArtifactRunner` if useful. Resolve the requested type against `DiscoveredType.FullName` case-insensitively. Do not silently fall back to short-name matching. No-match reports should still be generated with requested type, source files analysed, discovered type count, generated timestamps, and a clear no-match message. Duplicate full-name matches should produce an ambiguity section with project/source-path evidence rather than guessing.

The scoped report should include direct outbound dependencies from the root type, direct inbound dependants to the root type, concerns involving the root type, a compact Mermaid diagram centred on the type, and clear limitations. It must not claim runtime DI resolution, reflection or dynamic loading understanding, transitive dependency completeness, generated-code behaviour, or runtime call graph accuracy. Existing `class-dependencies.md` output behaviour should be preserved.


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

`configuration-inventory` is now an MVP-scope capability. It should produce `configuration-inventory.md` as a separate Markdown artifact. It is a static, evidence-backed inventory of visible configuration files, configuration values, configuration sections, transforms, source-code configuration usage, key reconciliation, and migration-relevant configuration concerns in a .NET codebase.

Intended command shape:

```bash
legacylens scan <path> --output-dir ./output --artifacts configuration-inventory
```

The report should identify visible configuration files, app settings, connection strings, custom sections, environment transforms, WCF configuration, ASP.NET/IIS sections, binding redirects, authentication and authorization settings, logging configuration, Entity Framework configuration, SMTP settings, JSON configuration values, and configuration API usage where discoverable. It should also map statically discoverable source-code configuration usage back to visible configured keys where possible, including literal `ConfigurationManager.AppSettings[...]`, `ConfigurationManager.AppSettings.Get(...)`, `ConfigurationManager.ConnectionStrings[...]`, `ConfigurationManager.ConnectionStrings.Get(...)`, and fully qualified `System.Configuration.ConfigurationManager` variants.

Detailed configuration findings should be grouped by project and source file, then by category within each file. This makes it easier for a .NET developer to find which file contains a setting. Per-file detail tables should use compact columns such as `Name`, `Value`, `Evidence`, and `Requires Review`, rather than repeating `Category` and `Source File` on every row.

Source-code configuration usage should be reported separately from configured values. Literal app setting and connection string access should preserve project name, source path, line number, evidence, key/name, resolution, and review status. Dynamic, computed, interpolated, concatenated, variable-based, or method-call-based key access should be classified as requiring review without inventing a key. Reconciliation should distinguish `Matched visible configuration entry`, `No visible configuration entry found`, `Dynamic key requires review`, and `No static source usage detected`. The phrase `No static source usage detected` must not be treated as proof that a configured key is unused.

Value semantics:

- `Value` means the discovered value with sensitive parts masked where needed.
- Use `N/A` for structural findings with no scalar value, such as file presence, section presence, binding redirects, transforms, `.settings` files, and `NuGet.config` file-level findings.
- Do not render missing scalar values as `Unknown` unless the analyzer genuinely tried and failed to determine a value.
- Preserve useful non-secret parts of connection-string-like values while masking embedded credentials.
- For AMQP/RabbitMQ URI values, prefer output such as `amqp://***:***@rabbitmq-dev:5672/sample` rather than full redaction where possible.

The capability must not claim to run the application, apply transforms, validate credentials, connect to external systems, prove production usage, prove a setting is used or unused, fully evaluate runtime configuration inheritance, resolve deployment-time substitutions, expose full secrets, or guarantee completeness. Sensitive values should be masked or redacted. Use cautious wording such as `Configuration evidence found`, `Static source usage found`, `Literal configuration key matched to visible configuration entry`, `No visible configuration entry found`, `No static source usage detected`, `Dynamic key could not be resolved statically`, `Requires review`, `May need migration`, `Configured setting`, and `Potential migration concern`.

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


## MVP interface-inventory addition

`interface-inventory` is now an MVP-scope capability. It should produce `interface-inventory.md` as a separate Markdown artifact. It is a static, evidence-backed interface and extension-point inventory for understanding available abstractions in unfamiliar .NET codebases.

Intended command shape:

```bash
legacylens scan <path> --output-dir ./output --artifacts interface-inventory
```

The artifact should analyse C# source files and visible configuration/XML files under discovered projects without building the target solution. It should discover interface declarations, implementations, static consumers, registration evidence, likely interface roles, possible extension points, interfaces with multiple implementations, interfaces with no static implementation found, interfaces with no static consumer found, and dynamic/configuration-driven wiring that requires review.

Registration evidence should include high-value static Microsoft DI patterns such as `AddSingleton`, `AddScoped`, `AddTransient`, and `TryAdd*`; legacy/third-party IoC patterns such as Castle Windsor, Autofac, Ninject, Unity, StructureMap, Simple Injector, LightInject, Lamar, and Common Service Locator where syntactically visible; and XML/configuration-driven evidence from Spring.NET, Castle Windsor XML, Unity XML, Enterprise Library/ObjectBuilder-style configuration, and custom object factory sections where feasible. Factory, reflection, assembly scanning, XML, alias, parent/child, profile-based, and service-locator patterns should be marked as requiring review. For Spring.NET, do not use XML comments, `<description>` text, root `<objects>` text, or arbitrary descendant text as matching input or evidence. Prefer meaningful object definitions, object `type`, `constructor-arg`, `property`, `factory-object`, `factory-method`, `parent`, `abstract`, alias-style wiring, and similar executable configuration. Simplify assembly-qualified XML type values from the type portion before the comma, so `SampleLegacyApp.Services.ICustomerService, SampleLegacyApp.Services` becomes `ICustomerService` and `SampleLegacyApp.Services.CustomerService, SampleLegacyApp.Services` becomes `CustomerService`.

The capability must not claim runtime completeness. It must not claim that an interface is definitely unused, definitely active at runtime, definitely registered, or definitely safe to implement. Use cautious wording such as `Static source evidence`, `Static configuration evidence`, `No static implementation found`, `No static consumer found`, `Registration evidence found`, `Dynamic wiring may exist`, `Configuration-driven wiring may exist`, `Requires review`, `Possible extension point`, `Likely role`, `Static analysis finding`, and `No static source usage detected`. XML/configuration-driven Spring.NET evidence remains static review evidence only and should stay marked as requiring review.

## MVP code complexity artifact addition

`code-complexity` is MVP scope as an optional static report artifact for identifying refactoring and review hotspots. Intended usage:

```bash
legacylens scan <path> --output-dir ./output --artifacts code-complexity
```

The generated file should be:

```text
code-complexity.md
```

The analyzer should consume `ScanContext.FileInventory` / `ScanFileInventory.CSharpFiles` and must not perform a separate recursive filesystem scan. It should use Roslyn syntax parsing without requiring solution build, project load, semantic models, package restore, compilation, or runtime execution. Complexity should start at `1` per supported member and increment for syntax-level decision points such as `if`, `else if`, loops, switch sections, switch expression arms, `catch`, ternary conditionals, logical `&&` and `||`, and pattern `when` clauses where discoverable.

The report should include member details plus type, namespace, project, and scan-root summaries. Suggested severity bands are `Low` 1-5, `Moderate` 6-10, `High` 11-20, and `Very High` 21+. The wording must remain cautious: findings are static discovery signals to help prioritise review, tests, simplification, and refactoring. They must not be described as exact Microsoft or Visual Studio code metrics, runtime risk, defect probability, test coverage, testability proof, complete maintainability assessment, or safe automatic refactoring advice.
