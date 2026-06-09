# LegacyLens.NET AI Context

This file is intended to be used as concise context for AI-assisted development chats.

## Product purpose

LegacyLens.NET is a standalone static discovery CLI for unfamiliar, legacy, and modern .NET codebases.

It scans source and configuration files to produce Markdown reports that help a developer understand structure, dependencies, package compatibility review signals, upgrade-readiness signals, upgrade-blocker decision signals, possible external dependency signals, data access inventory signals, EDMX analysis signals, legacy technology indicators, configuration concerns, and prioritised modernisation review areas.

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
--artifacts <value>     Optional artifact selection. MVP target includes upgrade-readiness, upgrade-blockers, external-dependencies, data-access, and edmx-analysis.
--upgrade-target <tfm>  Optional requested target framework for upgrade-readiness or upgrade-blockers wording.
-h, --help             Show help.
--version              Show version.
```

## Current output

By default, the report is generated at:

```text
<scan-path>/output/discovery-report.md
```

The main discovery report currently includes solution, project, target framework, package reference, assembly reference, project reference, WCF, Legacy ASP.NET, configuration, modernisation hint, modernisation review summary, and Mermaid dependency diagram sections. The MVP scope also includes package compatibility review, a separate `upgrade-readiness-report.md` artifact, a separate `upgrade-blockers.md` artifact, a separate `external-dependencies.md` artifact for identifying possible external runtime and build-time dependencies, a separate `data-access-inventory.md` artifact for identifying visible data access technologies, patterns, and migration concerns, and a separate `edmx-analysis.md` artifact for inspecting EF EDMX model contents and EF Core migration concern signals.

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

## MVP package compatibility review addition

Package compatibility review is now MVP scope. It should enrich package discovery with package version, project target framework, package target framework where available, source format, source path, and possible compatibility concerns. It should remain static and evidence-backed, and it should not be described as full NuGet restore, transitive dependency resolution, online package lookup, package asset inspection, or guaranteed compatibility checking against a destination framework.

## MVP upgrade-readiness addition

`upgrade-readiness` is now an MVP-scope capability. It should produce `upgrade-readiness-report.md` as a separate Markdown artifact. It is a static, evidence-backed upgrade readiness assessment for .NET legacy codebases.

It should show current project target frameworks, project-level upgrade candidates, possible upgrade concerns, package and assembly reference considerations, and configuration/runtime risks such as `System.Web`, WCF, EF6, `packages.config`, direct assembly references, `Web.config`, or legacy ASP.NET artifacts.

Intended command shape:

```bash
legacylens scan <path> --output-dir ./output --artifacts upgrade-readiness --upgrade-target net8.0
```

`--upgrade-target` is optional. If omitted, use general upgrade-readiness wording.

The capability must not claim to build the solution, run tests, restore packages, resolve transitive dependencies, inspect NuGet package assets, automatically migrate code, or guarantee compatibility with any destination framework. Use cautious wording such as `Possible concern`, `Requires review`, `Evidence found`, `May need migration work`, and `Likely upgrade consideration`.

## MVP upgrade-blockers addition

`upgrade-blockers` is now an MVP-scope capability. It should produce `upgrade-blockers.md` as a separate Markdown artifact. It is a static, evidence-backed blocker and decision report for .NET upgrade planning.

Where `upgrade-readiness` answers how ready the solution looks for upgrade, `upgrade-blockers` answers what visible blockers and decisions could stop or complicate the upgrade. It should group findings such as `System.Web`, legacy ASP.NET artifacts, WCF/System.ServiceModel, EF6/EDMX/data-access indicators, `packages.config`, direct assembly or local DLL references, configuration/runtime coupling, Windows-only/platform-specific APIs, and custom build/MSBuild behaviour into clear blocker categories.

Intended command shape:

```bash
legacylens scan <path> --output-dir ./output --artifacts upgrade-blockers --upgrade-target net8.0
```

`--upgrade-target` is optional. If omitted, use general upgrade-blocker wording.

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

## Design constraints

- Static-first discovery.
- The target solution does not need to build.
- Report findings should be evidence-backed where possible.
- Discovery signals should be honest and not pretend to be full migration advice.
- MVP usefulness is based on report quality, not exhaustive semantic analysis.
- Avoid duplicated, misleading, or materially low-value report findings.

## MVP definition

The MVP is complete when LegacyLens.NET can statically scan a representative legacy .NET solution and produce readable Markdown reports that help a developer identify the main structure, dependencies, package compatibility review signals, upgrade-readiness signals, upgrade-blocker decision signals, possible external dependency signals, data access inventory signals, EDMX analysis signals, legacy technology indicators, configuration concerns, and prioritised modernisation review areas.

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
