# MVP Scope

This document defines the MVP boundary for LegacyLens.NET.

## MVP Functionality

Current MVP functionality includes implemented capabilities and newly required MVP-scope additions. Newly required additions should not be described as shipped behaviour until the code and report output are updated:

- standalone `legacylens scan <path>` CLI command
- `--output` and `-o` report file selection
- `--output-dir` report directory selection
- `--format markdown` command option
- `--quiet` console output mode
- `--verbose` console output mode
- `--help`, `-h`, and `--version` command support
- CLI validation for missing commands, missing scan paths, unknown options, unsupported formats, and invalid option combinations
- static `.sln` discovery
- solution name discovery
- solution project membership discovery
- solution reporting in the generated Markdown report
- static `.csproj` discovery
- project name discovery
- target framework discovery
- project-to-project reference discovery
- assembly reference discovery from `<Reference />` entries
- NuGet package reference discovery from `<PackageReference />` entries and legacy `packages.config` files
- package version discovery from `<PackageReference />` entries where available
- package version discovery from legacy `packages.config` files where available
- package target framework discovery from legacy `packages.config` files where available
- package source format and source path reporting for package references
- static package compatibility review for upgrade planning, including possible compatibility concerns based on package, version, project target framework, and package target framework evidence
- Markdown discovery report generation
- target framework summary reporting in the generated Markdown report
- package reference summary reporting in the generated Markdown report
- package compatibility review reporting in the generated Markdown report
- Mermaid project dependency diagram generation
- WCF endpoint discovery from configuration files
- WCF binding configuration discovery from named endpoint binding configurations
- WCF endpoint security mode discovery
- WCF endpoint transport and message credential type discovery
- WCF endpoint timeout discovery from named binding configurations
- WCF endpoint message size and buffer limit discovery from named binding configurations
- WCF endpoint transfer mode discovery from named binding configurations
- WCF endpoint reader quota discovery from named binding configurations
- WCF metadata exchange endpoint detection
- WCF endpoint reporting
- WCF binding detail reporting
- WCF reader quota reporting
- WCF service behaviour discovery from configuration files
- WCF endpoint behaviour discovery from configuration files
- WCF service metadata setting discovery from `<serviceMetadata>`
- WCF service debug setting discovery from `<serviceDebug>`
- WCF service throttling setting discovery from `<serviceThrottling>`
- WCF endpoint `webHttp` behaviour discovery
- WCF behaviour reporting in the generated Markdown report
- WCF service contract discovery from C# source files
- WCF operation discovery from `[OperationContract]` methods
- WCF operation discovery scoped to the containing service contract interface
- WCF service contract reporting
- legacy ASP.NET artifact discovery from `.aspx`, `.ascx`, `.master`, `.asmx`, `.ashx`, and `Global.asax` files
- WebForms page discovery
- WebForms user control discovery
- WebForms master page discovery
- ASMX web service discovery
- ASP.NET HTTP handler discovery
- config-based ASP.NET HTTP module registration discovery from `web.config`
- config-based ASP.NET HTTP handler registration discovery from `web.config`
- warning-level request pipeline modernisation hints for discovered HTTP module and handler registrations
- `Global.asax` discovery
- ASP.NET MVC controller discovery from C# source files
- ASP.NET MVC action method discovery from C# source files
- ASP.NET MVC route attribute discovery from C# source files
- ASP.NET MVC action, filter, and security-related attribute discovery from C# source files
- ASP.NET Web API controller discovery from C# source files
- ASP.NET Web API action method discovery from C# source files
- ASP.NET Web API route attribute discovery from C# source files
- ASP.NET Web API action, filter, and security-related attribute discovery from C# source files
- ASP.NET MVC area registration discovery from C# source files
- ASP.NET route configuration discovery from `RouteConfig.cs`
- ASP.NET MVC application startup discovery from `Application_Start`
- ASP.NET MVC startup registration call discovery for `AreaRegistration.RegisterAllAreas()`, `RouteConfig.RegisterRoutes(...)`, `BundleConfig.RegisterBundles(...)`, and `FilterConfig.RegisterGlobalFilters(...)`
- ASP.NET Web API configuration discovery from `WebApiConfig.cs`
- ASP.NET Web API route registration call discovery for `MapHttpRoute(...)`
- ASP.NET Web API startup registration call discovery for `GlobalConfiguration.Configure(...)` and `WebApiConfig.Register(...)`
- ASP.NET MVC bundle configuration discovery from `BundleConfig.cs`
- ASP.NET MVC filter configuration discovery from `FilterConfig.cs`
- ASP.NET MVC dependency resolver registration discovery
- ASP.NET MVC custom controller factory registration discovery
- ASP.NET MVC global filter registration discovery
- ASP.NET MVC model binder registration discovery
- ASP.NET MVC value provider factory registration discovery
- ASP.NET Web API dependency resolver configuration discovery
- ASP.NET Web API formatter configuration discovery
- ASP.NET Web API message handler registration discovery
- ASP.NET Web API filter registration discovery
- ASP.NET Web API CORS registration discovery
- legacy ASP.NET artifact reporting in the generated Markdown report
- configuration file discovery from `app.config` and `web.config`
- `appSettings`, `connectionStrings`, and custom configuration section counting
- configuration file reporting in the generated Markdown report
- evidence-backed modernisation hint analysis
- modernisation hints for old .NET Framework target frameworks
- modernisation hints for missing target framework declarations
- modernisation hints for selected legacy or review-worthy packages
- modernisation hints for package compatibility concerns that may affect upgrade planning
- modernisation hints for legacy ASP.NET, `System.Web` assembly references, discovered legacy ASP.NET artifacts, ASP.NET MVC controllers, ASP.NET MVC actions, ASP.NET MVC route attributes, ASP.NET MVC action attributes, ASP.NET MVC area registrations, ASP.NET route configuration, ASP.NET MVC application startup, ASP.NET MVC startup registration calls, ASP.NET MVC bundle configuration, ASP.NET MVC filter configuration, ASP.NET MVC dependency resolver registration, ASP.NET MVC controller factory registration, ASP.NET MVC global filter registration, ASP.NET MVC model binder registration, ASP.NET MVC value provider factory registration, ASP.NET HTTP module registrations, ASP.NET HTTP handler registrations, ASP.NET Web API controllers, ASP.NET Web API actions, ASP.NET Web API route attributes, ASP.NET Web API action attributes, ASP.NET Web API configuration, ASP.NET Web API route registration, ASP.NET Web API startup registration, ASP.NET Web API dependency resolver configuration, ASP.NET Web API formatter configuration, ASP.NET Web API message handler registration, ASP.NET Web API filter registration, and ASP.NET Web API CORS registration
- modernisation hints for WCF endpoints, selected WCF binding types, endpoint binding configurations, security modes, transport credential types, timeout settings, message size and buffer limits, transfer modes, reader quotas, metadata exchange endpoints, service contracts, service behaviours, endpoint behaviours, metadata publishing settings, debug settings, throttling settings, and REST-style `webHttp` endpoint behaviours
- modernisation hints for configuration-heavy applications
- modernisation hint reporting with evidence, confidence, source, and reason in the generated Markdown report
- modernisation hint de-duplication after evidence metadata has been attached
- WCF binding hint wording that includes available endpoint contract and binding configuration details so multiple endpoints on the same service can be distinguished
- modernisation review summary generation from detailed modernisation hints
- modernisation review summary prioritisation using highest severity, review-area priority, hint counts, and review area name
- modernisation review summary reporting in the generated Markdown report
- output file generation under the `output/` directory
- optional `--artifacts upgrade-readiness` command support for producing the upgrade-readiness artifact
- optional `--upgrade-target <tfm>` command support for upgrade-readiness report context
- static upgrade-readiness analysis for upgrade planning
- upgrade-readiness report generation as `upgrade-readiness-report.md`
- upgrade-readiness current project target reporting
- upgrade-readiness project-level readiness classification using `Lower risk candidate`, `Moderate review required`, `Higher risk / review first`, and `Unknown`
- upgrade-readiness possible concern reporting based on static evidence
- upgrade-readiness package upgrade consideration reporting where package metadata exists
- upgrade-readiness assembly reference consideration reporting where assembly references exist
- upgrade-readiness configuration and runtime consideration reporting where configuration, WCF, or legacy ASP.NET evidence exists
- upgrade-readiness notes and limitations explaining static no-build analysis

## MVP Exit Criteria

The MVP should be considered complete when the tool can produce a useful static discovery report for the sample legacy solution without requiring that solution to build.

The MVP exit criteria are:

- The CLI can scan the sample legacy solution successfully.
- The generated Markdown report includes solution, project, target framework, package reference, package compatibility review, assembly reference, project reference, WCF, Legacy ASP.NET, configuration, modernisation hint, and modernisation review summary sections. The MVP can also produce a separate `upgrade-readiness-report.md` artifact.
- The report identifies the main modernisation review areas clearly enough for a developer to decide where to investigate first.
- The package compatibility review shows package name, version where available, project target framework, package target framework where available, source format, source path, and possible compatibility concern without claiming to perform full NuGet compatibility resolution.
- Modernisation hints include useful evidence metadata where a clear source exists, including evidence kind, evidence name, confidence, source path, and reason.
- The report does not contain known duplicated, misleading, or materially low-value findings that would confuse a reader.
- Existing automated tests pass.
- The upgrade-readiness report includes current project targets, project-level readiness classifications, possible upgrade concerns, package upgrade considerations, assembly reference considerations, configuration/runtime considerations, and clear static-analysis limitations.
- The README reflects the actual current report output and does not describe speculative MVP behaviour as already implemented.

### CLI command contract

Status: Implemented

- Provide a standalone `legacylens` command
- Support `legacylens scan <path>`
- Support `--output` and `-o` for writing to a specific Markdown file
- Support `--output-dir` for writing `discovery-report.md` to a selected directory
- Support `--format markdown`
- Reject unsupported formats
- Support `--quiet`
- Support `--verbose`
- Support `--help` and `-h`
- Support `--version`
- Reject invalid combinations such as `--output` with `--output-dir`
- Reject invalid combinations such as `--quiet` with `--verbose`

Further discovery refinements are post-MVP unless the current sample report exposes a clear false positive, false negative, duplicated finding, misleading evidence source, or confusing prioritisation issue that materially reduces report usefulness.

## Not MVP Blockers

The following are not blockers for the MVP:

- deeper semantic parsing of every possible WCF usage pattern
- deeper HTTP module or HTTP handler implementation analysis
- exhaustive ASP.NET MVC or Web API behaviour analysis
- full migration recommendations or automated migration planning
- automatic migration execution or definitive pass/fail upgrade compatibility decisions
- full NuGet restore, transitive dependency resolution, package asset inspection, or guaranteed package compatibility checks
- HTML report output
- support for every possible legacy project or configuration edge case
- deeper analysis that is not required to fix a clear report-quality issue in the current sample output

These items may be valuable later, but they should be treated as post-MVP improvements unless a realistic sample report proves that one of them is needed to avoid a materially misleading MVP report.

## MVP Completion Statement

The MVP is not intended to be a complete migration analyser.

The MVP is complete when LegacyLens.NET can statically scan a representative legacy .NET solution and produce a readable Markdown report that helps a developer identify the main structure, dependencies, legacy technology indicators, configuration concerns, and prioritised modernisation review areas.

Once that is achieved, further work should be treated as post-MVP unless it fixes a specific report-quality defect.
