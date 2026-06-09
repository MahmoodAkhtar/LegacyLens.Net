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
- optional `--artifacts upgrade-blockers` command support for producing the upgrade-blockers artifact
- optional `--artifacts external-dependencies` command support for producing the external-dependencies artifact
- optional `--artifacts data-access` command support for producing the data-access artifact
- optional `--artifacts edmx-analysis` command support for producing the edmx-analysis artifact
- optional `--upgrade-target <tfm>` command support for upgrade-readiness and upgrade-blockers report context
- static upgrade-readiness analysis for upgrade planning
- upgrade-readiness report generation as `upgrade-readiness-report.md`
- upgrade-readiness current project target reporting
- upgrade-readiness project-level readiness classification using `Lower risk candidate`, `Moderate review required`, `Higher risk / review first`, and `Unknown`
- upgrade-readiness possible concern reporting based on static evidence
- upgrade-readiness package upgrade consideration reporting where package metadata exists
- upgrade-readiness assembly reference consideration reporting where assembly references exist
- upgrade-readiness configuration and runtime consideration reporting where configuration, WCF, or legacy ASP.NET evidence exists
- upgrade-readiness notes and limitations explaining static no-build analysis
- static upgrade-blockers analysis for upgrade planning
- upgrade-blockers report generation as `upgrade-blockers.md`
- upgrade-blockers blocker overview reporting with priority, category, impact, and evidence count
- upgrade-blockers decision-oriented reporting for visible blockers and migration decisions
- upgrade-blockers category grouping for Legacy ASP.NET/System.Web, WCF/ServiceModel, EF6/EDMX/Data Access, Package Management, Direct Assembly References, Configuration/Runtime Coupling, Windows-only/Platform-specific APIs, Custom Build/MSBuild Behaviour, and Unknown/Requires Manual Review where evidence exists
- upgrade-blockers evidence reporting using existing discovered project, package, assembly, WCF, legacy ASP.NET, configuration, and modernisation/package review evidence where available
- upgrade-blockers notes and limitations explaining static no-build analysis and that blockers mean “requires review”, not “cannot be upgraded”
- static external-dependencies analysis for identifying possible runtime and build-time dependencies outside the repository
- external-dependencies report generation as `external-dependencies.md`
- external-dependencies category grouping for Database, HTTP/API, WCF/Service Endpoint, Messaging/Queue, File System/File Share, Email/SMTP, Cache/Distributed State, Authentication/Identity Provider, Cloud Service, Private Package Feed, External Assembly/Vendor DLL, and Unknown/Requires Review where evidence exists
- external-dependencies evidence reporting using existing discovered configuration, WCF, package, assembly, project, source, and private package feed evidence where available
- external-dependencies masking or redaction for sensitive values such as passwords, API keys, tokens, SAS tokens, access keys, client secrets, private feed credentials, and connection string secrets
- external-dependencies suggested team questions and notes/limitations explaining static no-build analysis and that findings mean “requires confirmation”, not “verified production dependency”
- static data-access analysis for identifying visible data access technologies, patterns, and migration concerns
- data-access report generation as `data-access-inventory.md`
- data-access category grouping for Connection String, Entity Framework 6, Entity Framework Core, EDMX / ObjectContext, ADO.NET, Dapper, NHibernate, LINQ to SQL, Raw SQL, Stored Procedure, Repository Pattern, Unit of Work Pattern, Database Provider, Migration Artifact, and Unknown / Requires Review where evidence exists
- data-access evidence reporting using existing discovered configuration, package, assembly, project, source, EDMX/T4/DBML, and migration-folder evidence where available
- data-access masking or redaction for sensitive values such as database passwords, user names where appropriate, access tokens, API keys, and embedded credentials
- data-access suggested files to review first, migration considerations, suggested team questions, and notes/limitations explaining static no-build analysis and that findings mean “requires review”, not “verified runtime usage”
- static edmx-analysis for inspecting Entity Framework `.edmx` files used by EF6 Database First or Model First projects
- edmx-analysis report generation as `edmx-analysis.md`
- edmx-analysis discovery of EDMX files under scanned projects and association with the nearest discovered project where possible
- edmx-analysis parsing of EDMX XML using defensive, namespace-tolerant static inspection
- edmx-analysis reporting for CSDL conceptual model details such as entities, entity sets, keys, properties, associations, navigation properties, complex types, and function imports
- edmx-analysis reporting for SSDL storage model details such as schemas, tables/views, columns, keys, store functions, and defining queries
- edmx-analysis reporting for MSL mapping details such as entity-to-table mappings, scalar property mappings, association mappings, function import mappings, modification function mappings, and query views
- edmx-analysis reporting for designer metadata and companion generated files such as T4 templates, `.Designer.cs`, and generated context/model files where discoverable
- edmx-analysis upgrade concern reporting for EDMX usage, stored procedure/function mappings, complex types, query-backed entities, defining queries, designer metadata, generated files, malformed EDMX files, and EF Core migration review points
- edmx-analysis notes/limitations explaining static no-build analysis and that findings do not represent database validation, EF Core model generation, automatic conversion, or compatibility guarantees

## MVP Exit Criteria

The MVP should be considered complete when the tool can produce a useful static discovery report for the sample legacy solution without requiring that solution to build.

The MVP exit criteria are:

- The CLI can scan the sample legacy solution successfully.
- The generated Markdown report includes solution, project, target framework, package reference, package compatibility review, assembly reference, project reference, WCF, Legacy ASP.NET, configuration, modernisation hint, and modernisation review summary sections. The MVP can also produce separate `upgrade-readiness-report.md`, `upgrade-blockers.md`, `external-dependencies.md`, `data-access-inventory.md`, and `edmx-analysis.md` artifacts.
- The report identifies the main modernisation review areas clearly enough for a developer to decide where to investigate first.
- The package compatibility review shows package name, version where available, project target framework, package target framework where available, source format, source path, and possible compatibility concern without claiming to perform full NuGet compatibility resolution.
- Modernisation hints include useful evidence metadata where a clear source exists, including evidence kind, evidence name, confidence, source path, and reason.
- The report does not contain known duplicated, misleading, or materially low-value findings that would confuse a reader.
- Existing automated tests pass.
- The upgrade-readiness report includes current project targets, project-level readiness classifications, possible upgrade concerns, package upgrade considerations, assembly reference considerations, configuration/runtime considerations, and clear static-analysis limitations.
- The upgrade-blockers report includes a blocker overview, grouped blocker details, impact labels, evidence, why each blocker matters, decisions required, suggested review order, and clear static-analysis limitations.
- The data-access inventory includes an analysis scope, data access overview, projects with data access indicators, connection string/provider information with masked sensitive values, ORM and data access technology evidence, suggested files to review first, migration considerations, suggested team questions, and clear static-analysis limitations.
- The external-dependencies report includes an analysis scope, dependency overview, grouped dependency sections, source/evidence details, confirmation flags, suggested team questions, sensitive value masking, and clear static-analysis limitations.
- The edmx-analysis report includes summary counts, discovered EDMX files, conceptual model details, storage model details, associations, function imports and store functions, mapping details, companion generated files, upgrade concerns, and clear static-analysis limitations.
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
- automatic migration execution, definitive pass/fail upgrade compatibility decisions, or claims that a blocker proves migration is impossible
- full NuGet restore, transitive dependency resolution, package asset inspection, or guaranteed package compatibility checks
- runtime dependency mapping, network scanning, credential validation, production infrastructure inspection, or guaranteed complete external dependency inventory
- EDMX-to-EF Core automatic conversion, live database validation, EF Core model generation, or guaranteed EDMX migration compatibility
- HTML report output
- support for every possible legacy project or configuration edge case
- deeper analysis that is not required to fix a clear report-quality issue in the current sample output

These items may be valuable later, but they should be treated as post-MVP improvements unless a realistic sample report proves that one of them is needed to avoid a materially misleading MVP report.

## MVP Completion Statement

The MVP is not intended to be a complete migration analyser.

The MVP is complete when LegacyLens.NET can statically scan a representative legacy .NET solution and produce readable Markdown reports that help a developer identify the main structure, dependencies, legacy technology indicators, configuration concerns, prioritised modernisation review areas, upgrade planning artifacts such as readiness and blocker/decision reports, and possible external runtime or build-time dependencies that require confirmation, and EDMX models that require EF Core migration review.

Once that is achieved, further work should be treated as post-MVP unless it fixes a specific report-quality defect.
