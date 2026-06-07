# LegacyLens.NET AI Context

This file is intended to be used as concise context for AI-assisted development chats.

## Product purpose

LegacyLens.NET is a standalone static discovery CLI for unfamiliar, legacy, and modern .NET codebases.

It scans source and configuration files to produce a Markdown discovery report that helps a developer understand structure, dependencies, package compatibility review signals, upgrade-readiness signals, legacy technology indicators, configuration concerns, and prioritised modernisation review areas.

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
-h, --help             Show help.
--version              Show version.
```

## Current output

By default, the report is generated at:

```text
<scan-path>/output/discovery-report.md
```

The main discovery report currently includes solution, project, target framework, package reference, assembly reference, project reference, WCF, Legacy ASP.NET, configuration, modernisation hint, modernisation review summary, and Mermaid dependency diagram sections. The MVP scope also includes package compatibility review and a separate `upgrade-readiness-report.md` artifact for upgrade planning.

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

## Design constraints

- Static-first discovery.
- The target solution does not need to build.
- Report findings should be evidence-backed where possible.
- Discovery signals should be honest and not pretend to be full migration advice.
- MVP usefulness is based on report quality, not exhaustive semantic analysis.
- Avoid duplicated, misleading, or materially low-value report findings.

## MVP definition

The MVP is complete when LegacyLens.NET can statically scan a representative legacy .NET solution and produce a readable Markdown report that helps a developer identify the main structure, dependencies, package compatibility review signals, upgrade-readiness signals, legacy technology indicators, configuration concerns, and prioritised modernisation review areas.

Further work should be treated as post-MVP unless it fixes a specific report-quality defect.

Package compatibility review is now MVP scope, but it should remain static and evidence-backed. It should not be described as full NuGet restore, transitive dependency resolution, online package lookup, package asset inspection, or guaranteed compatibility checking against a destination framework.

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
- `docs/report-output.md` contains console, discovery report, and upgrade-readiness report examples.
- `docs/discovery-capabilities.md` contains detailed scanner capability information.
- `docs/architecture.md` contains repository and project structure.
- `docs/mvp.md` defines MVP scope and exit criteria.
- `docs/roadmap.md` captures post-MVP direction.
