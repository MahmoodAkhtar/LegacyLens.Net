# Roadmap

This document captures forward-looking work and post-MVP refinement rules for LegacyLens.NET.

## Development Roadmap

The roadmap is maintained as a forward-looking plan. It separates implemented discovery capability from conditional MVP quality gates and post-MVP ideas so future work does not become an endless blocker to the first release.

### Post-MVP Refinement Rules

After the MVP exit criteria are met, additional discovery work should be prioritised only when it satisfies at least one of these rules:

- It fixes a confirmed false positive in a generated report.
- It fixes a confirmed false negative for a high-value migration signal.
- It improves evidence precision where the current evidence would send a reader to the wrong source.
- It reduces duplicated or noisy findings that make the report harder to use.
- It improves prioritisation where the review summary ranks less actionable areas above clearly higher-value migration risks.
- It adds support for a realistic legacy pattern found in an external sample application or real-world codebase.
- It improves evidence for external runtime or build-time dependencies that materially affect migration, deployment, testing, onboarding, or local development.

Ideas that do not satisfy one of these rules should remain post-MVP backlog items.

### Conditional MVP Quality Gates

Additional discovery, analysis, or reporting work should block the MVP only when the current generated report proves there is a specific, material report-quality defect.

The MVP should not remain open merely because deeper analysis could be added later. Further work is required before MVP only when it fixes a clear false positive, false negative, duplicated finding, misleading evidence source, or confusing prioritisation issue in the current sample output.

### Step 1: Static solution and project discovery

Status: Implemented

- Discover `.sln` files
- Read solution name
- Read C# project paths referenced by solution files
- Discover `.csproj` files
- Read project name
- Read target framework
- Read project references
- Read assembly references from `<Reference />` entries
- Read package references from `<PackageReference />` entries and legacy `packages.config` files
- Read package versions where available
- Read package target framework metadata from `packages.config` where available
- Track package source format and source path for package compatibility review

### Step 2: Markdown report generation

Status: Implemented

- Generate `output/discovery-report.md`
- Include summary counts
- Include solution summary
- Include solution table
- Include project table
- Include target framework summary
- Include package reference summary
- Include package compatibility review section
- Include project references
- Include assembly references
- Include package references
- Include package compatibility review information
- Include legacy ASP.NET artifact information in the generated Markdown report
- Include config-based ASP.NET HTTP module and handler registrations in the legacy ASP.NET artifact report section
- Include configuration file details
- Include WCF binding detail sections
- Include WCF reader quota sections
- Include WCF behaviour detail sections
- Include modernisation review summary

### Step 3: Dependency diagram generation

Status: Implemented

- Generate Mermaid dependency graph
- Include graph in Markdown report

### Step 4: WCF configuration and service contract discovery

Status: Implemented with conditional quality gates

Implemented:

- Detect WCF configuration in `app.config` and `web.config`
- Detect configured WCF endpoints
- Report service name, address, binding, binding configuration, security mode, transport credential type, message credential type, metadata exchange indicator, contract, and config file path
- Resolve selected details from named WCF binding configurations
- Detect WCF binding timeout settings from named binding configurations
- Detect WCF message size and buffer limits from named binding configurations
- Detect WCF transfer mode from named binding configurations
- Detect WCF reader quota settings from named binding configurations
- Report WCF binding details and reader quotas in the generated Markdown report
- Detect WCF service behaviours from `<serviceBehaviors>`
- Detect WCF endpoint behaviours from `<endpointBehaviors>`
- Detect WCF service metadata settings such as `httpGetEnabled` and `httpsGetEnabled`
- Detect WCF service debug settings such as `includeExceptionDetailInFaults`
- Detect WCF service throttling settings such as `maxConcurrentCalls`, `maxConcurrentSessions`, and `maxConcurrentInstances`
- Detect WCF endpoint `webHttp` behaviour indicators
- Report WCF behaviours in the generated Markdown report
- Print WCF behaviours in the CLI output
- Detect WCF metadata exchange endpoints from `IMetadataExchange` contracts and `mex*` bindings
- Detect WCF service contracts from C# source files
- Detect WCF operations marked with `[OperationContract]`
- Scope detected WCF operations to their containing service contract interface
- Report contract name, operation names, and source file path

Post-MVP ideas:

- Deeper WCF configuration analysis, such as diagnostics, custom bindings, client endpoint configuration, service hosting activation details, credential behaviours, authorization behaviours, message inspectors, and custom behaviour extension details.

### Step 5: Risk and modernisation hints

Status: Implemented with conditional quality gates; package compatibility review is an MVP-scope addition to complete.

Implemented:

- Identify old .NET Framework target frameworks such as `net48`
- Identify missing target framework declarations
- Identify WCF-related packages such as `System.ServiceModel.*`
- Identify classic Entity Framework package usage
- Identify `Newtonsoft.Json` usage as an informational review item
- Highlight projects with several direct project references
- Highlight discovered WCF endpoints
- Highlight selected WCF binding types, including `basicHttpBinding`, `netTcpBinding`, `wsHttpBinding`, and `netMsmqBinding`
- Highlight WCF endpoints with missing binding information
- Highlight WCF endpoints that use named binding configurations
- Highlight WCF endpoint security modes
- Highlight WCF transport credential types
- Highlight WCF timeout settings
- Highlight WCF message size and buffer limits
- Highlight WCF transfer modes, including streaming transfer modes
- Highlight WCF reader quota settings
- Highlight WCF metadata exchange endpoints
- Highlight discovered WCF service contracts
- Highlight discovered WCF service behaviours
- Highlight discovered WCF endpoint behaviours
- Highlight WCF service metadata publishing settings
- Highlight WCF debug exception detail settings
- Highlight WCF service throttling settings
- Highlight WCF REST-style `webHttp` endpoint behaviours
- Identify legacy ASP.NET indicators from `System.Web` and `System.Web.*` assembly references
- Identify WebForms pages as legacy ASP.NET migration risk indicators
- Identify ASMX web services as legacy ASP.NET migration risk indicators
- Identify WebForms user controls, master pages, and HTTP handlers as legacy ASP.NET review items
- Identify `Global.asax` application files as ASP.NET lifecycle and startup review items
- Identify ASP.NET MVC controllers as legacy ASP.NET review items
- Identify ASP.NET MVC actions as request-handling review items
- Identify ASP.NET MVC route attributes as endpoint routing review items
- Identify ASP.NET MVC action, filter, and security-related attributes as behaviour migration review items
- Identify ASP.NET MVC area registration classes as ASP.NET routing and feature-boundary review items
- Identify ASP.NET route configuration files as ASP.NET routing migration review items
- Identify ASP.NET MVC application startup methods as ASP.NET startup and hosting review items
- Identify ASP.NET MVC startup registration calls for area, route, bundle, and filter registration
- Identify ASP.NET MVC bundle configuration and bundle registration as static asset migration review items
- Identify ASP.NET MVC filter configuration and global filter registration as cross-cutting request behaviour review items
- Identify ASP.NET Web API controllers as HTTP API migration review items
- Identify ASP.NET Web API actions as endpoint behaviour review items
- Identify ASP.NET Web API route attributes as endpoint routing review items
- Identify ASP.NET Web API action, filter, and security-related attributes as behaviour migration review items
- Identify ASP.NET Web API configuration files as API startup and routing review items
- Identify ASP.NET Web API route registration calls as conventional API routing review items
- Identify ASP.NET Web API startup registration calls as API startup and hosting review items
- Identify ASP.NET HTTP module registrations from `web.config` as request pipeline review items
- Identify ASP.NET HTTP handler registrations from `web.config` as request pipeline review items
- Identify configuration-heavy application indicators from `app.config` and `web.config`
- Identify large `appSettings` usage
- Identify connection strings as external data dependency indicators
- Identify custom configuration sections
- Include modernisation hints in the generated Markdown report
- Add evidence kind, evidence name, source path, and confidence metadata to modernisation hints
- De-duplicate modernisation hints after evidence metadata is attached
- Include available WCF endpoint contract and binding configuration details in WCF binding hint findings
- Report modernisation hint evidence, confidence, and source in the generated Markdown report
- Map modernisation hint evidence to projects, package references, assembly references, WCF endpoints, WCF service contracts, WCF behaviours, legacy ASP.NET artifacts, configuration files, or analysis summaries
- Prefer the most specific matching legacy ASP.NET artifact evidence when multiple artifact names match a hint
- Group detailed modernisation hints into higher-level modernisation review areas
- Rank modernisation review areas by highest severity, review-area priority, and hint counts
- Report prioritised modernisation review areas in the generated Markdown report
- Print prioritised modernisation review areas in the CLI output

---

### Step 5a: Package compatibility review for upgrade planning

Status: MVP scope addition

MVP scope:

- Preserve package id, version, source format, source path, and package target framework where available.
- Report project target framework alongside each package reference.
- Add a `Package Compatibility Review` section to the Markdown report.
- Include possible compatibility concerns for selected package patterns such as `System.ServiceModel.*`, `EntityFramework`, `Newtonsoft.Json`, missing package versions, and mismatches between project target framework and `packages.config` package target framework.
- Feed package compatibility findings into the existing dependency review area.
- Map package compatibility evidence to package references with package id, version, source path, source format, and package target framework where available.
- Keep wording evidence-backed and static: the tool should say that a package may need review, not that it is definitely compatible or incompatible with a destination framework.

Out of scope for MVP:

- NuGet restore.
- Transitive dependency resolution.
- Online package lookup.
- Package asset inspection.
- Automated upgrade recommendations.
- Guaranteed compatibility checks against .NET 8, .NET 9, .NET 10, or any future destination framework.

---

### Step 5b: Upgrade readiness report for upgrade planning

Status: MVP scope addition

MVP scope:

- Add an `upgrade-readiness` capability that can produce `upgrade-readiness-report.md`.
- Use existing static discovery evidence where possible, including project targets, project references, package metadata, assembly references, WCF findings, legacy ASP.NET artifacts, configuration files, and existing modernisation hints.
- Support optional upgrade target context, for example `--upgrade-target net8.0`, without claiming guaranteed compatibility.
- Classify project-level readiness using `Lower risk candidate`, `Moderate review required`, `Higher risk / review first`, and `Unknown`.
- Report possible upgrade concerns with supporting evidence and cautious wording.
- Include package upgrade considerations where package data exists.
- Include assembly reference considerations where assembly reference data exists.
- Include configuration and runtime considerations for `app.config`, `web.config`, WCF, legacy ASP.NET, connection strings, custom configuration sections, and related evidence.
- Include suggested review order and notes/limitations.
- Add unit tests for analyzer rules and Markdown output.

Out of scope for MVP:

- Building the solution.
- Running the application or tests.
- NuGet restore.
- Transitive dependency resolution.
- Online package lookup.
- NuGet package asset inspection.
- Automatic migration.
- Definitive pass/fail compatibility results for `net8.0`, `net10.0`, or any other destination framework.

Implementation should be incremental. If artifact selection is not yet generalised, add only enough command support to produce `output/upgrade-readiness-report.md` without over-engineering the CLI.

### Step 5c: Upgrade blockers report for upgrade planning

Status: MVP scope addition

MVP scope:

- Add an `upgrade-blockers` capability that can produce `upgrade-blockers.md`.
- Use existing static discovery evidence where possible, including project targets, package metadata, assembly references, direct DLL or `HintPath` references where available, WCF findings, legacy ASP.NET artifacts, configuration files, existing modernisation hints, and package compatibility/static package review information.
- Support optional upgrade target context, for example `--upgrade-target net8.0`, without claiming guaranteed compatibility.
- Group visible blockers into focused categories such as `Legacy ASP.NET / System.Web`, `WCF / ServiceModel`, `EF6 / EDMX / Data Access`, `Package Management`, `Direct Assembly References`, `Configuration / Runtime Coupling`, `Windows-only / Platform-specific APIs`, `Custom Build / MSBuild Behaviour`, and `Unknown / Requires Manual Review`.
- Assign impact labels using `High`, `Medium`, `Low`, or `Unknown`.
- Report why each blocker matters, the supporting evidence, and the migration decisions required.
- Include suggested review order and notes/limitations.
- Add unit tests for analyzer rules and Markdown output.

Out of scope for MVP:

- Building the solution.
- Running the application or tests.
- NuGet restore.
- Transitive dependency resolution.
- Online package lookup.
- NuGet package asset inspection.
- Automatic migration.
- Definitive pass/fail compatibility results for `net8.0`, `net10.0`, or any other destination framework.
- Claims that a blocker proves the project cannot be upgraded.
- Rewrite recommendations without supporting evidence.

Implementation should be incremental. If artifact selection is not yet generalised, add only enough command support to produce `output/upgrade-blockers.md` without over-engineering the CLI. The report should be more focused and decision-oriented than `upgrade-readiness-report.md`, not a duplicate of it.


### Step 5d: External dependencies inventory for migration, deployment, and onboarding

Status: MVP scope addition

MVP scope:

- Add an `external-dependencies` capability that can produce `external-dependencies.md`.
- Use existing static discovery evidence where possible, including configuration files, connection strings, app settings, WCF endpoints, package references, assembly references, direct DLL or `HintPath` evidence where available, private package feed configuration where available, and low-risk source string evidence where feasible.
- Group possible dependencies into focused categories such as `Database`, `HTTP / API`, `WCF / Service Endpoint`, `Messaging / Queue`, `File System / File Share`, `Email / SMTP`, `Cache / Distributed State`, `Authentication / Identity Provider`, `Cloud Service`, `Private Package Feed`, `External Assembly / Vendor DLL`, and `Unknown / Requires Review`.
- Report source/evidence, source file, project name where applicable, notes, optional confidence, and whether each finding requires confirmation.
- Mask or redact sensitive values such as passwords, API keys, tokens, SAS tokens, access keys, client secrets, private feed credentials, and connection string secrets.
- Include suggested questions to ask the team and notes/limitations.
- Add unit tests for analyzer rules and Markdown output, including secret masking and empty/no-findings handling.

Out of scope for MVP:

- Connecting to databases, APIs, queues, file shares, cloud services, Redis, SMTP, or package feeds.
- Validating credentials, URLs, server existence, network reachability, queue existence, or production usage.
- Running the application, executing code, or inspecting production infrastructure.
- Proving that a dependency is active, proving that a dependency is unused, or guaranteeing completeness.
- Printing full secrets or raw sensitive values.

Implementation should be incremental. If artifact selection is not yet generalised, add only enough command support to produce `output/external-dependencies.md` without over-engineering the CLI. The report should be distinct from configuration inventory, upgrade readiness, upgrade blockers, and data-access inventory; its focus is systems and resources outside the repository that may affect runtime, build, migration, deployment, testing, or onboarding.


### Step 5e: Data access inventory for migration and refactoring planning

Status: MVP scope addition

MVP scope:

- Add a `data-access` capability that can produce `data-access-inventory.md`.
- Use existing static discovery evidence where possible, including project targets, package metadata, assembly references, configuration files, connection strings, provider names, source file indicators, EDMX/T4/DBML file evidence, and existing modernisation or package review findings.
- Identify visible data access categories such as Connection String, Entity Framework 6, Entity Framework Core, EDMX / ObjectContext, ADO.NET, Dapper, NHibernate, LINQ to SQL, Raw SQL, Stored Procedure, Repository Pattern, Unit of Work Pattern, Database Provider, Migration Artifact, and Unknown / Requires Review where evidence exists.
- Mask or redact sensitive values in connection strings and settings.
- Report projects with data access indicators, connection strings, ORM/data access technologies, EF/EDMX details, DbContext/ObjectContext candidates, repository and unit-of-work candidates, raw SQL and stored procedure indicators, database provider indicators, suggested files to review first, migration considerations, suggested questions, and notes/limitations.
- Add unit tests for analyzer rules and Markdown output.

Out of scope for MVP:

- Connecting to databases.
- Validating credentials or connection strings.
- Executing SQL.
- Parsing or validating full SQL syntax.
- Discovering or comparing live database schemas.
- Running EF migrations.
- Scaffolding EF Core models.
- Reverse-engineering databases.
- Proving runtime usage.
- Proving that repositories, queries, or stored procedures are unused.
- Automatically migrating EF6, EDMX, NHibernate, Dapper, or ADO.NET code.
- Guaranteed EF6-to-EF Core or package compatibility results.

Implementation should be incremental. If artifact selection is not yet generalised, add only enough command support to produce `output/data-access-inventory.md` without over-engineering the CLI.

### Step 5f: EDMX analysis report for EF Core migration planning

Status: MVP scope addition

MVP scope:

- Add an `edmx-analysis` capability that can produce `edmx-analysis.md`.
- Discover `.edmx` files under scanned project folders and associate each EDMX file with the nearest discovered project where possible.
- Parse EDMX XML defensively using `System.Xml.Linq` and namespace-tolerant local-name matching for common EDMX, CSDL, SSDL, and MSL namespace versions.
- Identify whether the EDMX contains conceptual model, storage model, mapping model, and designer metadata sections.
- Extract conceptual model details such as entity types, entity sets, key properties, property counts, navigation-property counts, associations, complex types, and function imports.
- Extract storage model details such as store entity sets, schemas, tables/views, column counts, store functions, parameters, and defining-query indicators.
- Extract mapping details such as entity set mappings, entity type mappings, mapping fragments, scalar property counts, association set mappings, function import mappings, modification function mappings, and query views.
- Detect companion generated files such as `.tt`, `.Designer.cs`, generated context files, and generated model files where discoverable.
- Produce evidence-backed upgrade concerns for EDMX usage, stored procedure/function mappings, modification function mappings, query views, defining queries, complex types, designer metadata, companion generated files, and malformed/unreadable EDMX files.
- Add an `edmx-analysis.md` Markdown writer with Summary, EDMX Files, Upgrade Concerns, Conceptual Model, Storage Model, Associations, Function Imports and Store Functions, Mapping Details, Companion Generated Files, and Notes sections.
- Add unit tests for analyzer rules, namespace-tolerant parsing, malformed EDMX handling, companion file detection, and Markdown output.

Out of scope for MVP:

- Connecting to a database.
- Validating the EDMX against a live database or schema.
- Generating EF Core models.
- Converting EDMX to EF Core.
- Running NuGet restore.
- Building the solution.
- Guaranteeing migration compatibility.
- Full semantic understanding of custom T4 templates.
- Claiming that all EF Core equivalents are direct one-to-one replacements.

Implementation should be incremental. If artifact selection is not yet generalised, add only enough command support to produce `output/edmx-analysis.md` without over-engineering the CLI.


### Step 6: Legacy ASP.NET artifact discovery

Status: Implemented with conditional quality gates

Implemented:

- Detect `.aspx` WebForms pages
- Detect `.ascx` WebForms user controls
- Detect `.master` WebForms master pages
- Detect `.asmx` ASMX web services
- Detect `.ashx` ASP.NET HTTP handlers
- Detect ASP.NET HTTP module registrations from `system.web/httpModules` in `web.config`
- Detect ASP.NET HTTP module registrations from `system.webServer/modules` in `web.config`
- Detect ASP.NET HTTP handler registrations from `system.web/httpHandlers` in `web.config`
- Detect ASP.NET HTTP handler registrations from `system.webServer/handlers` in `web.config`
- Detect `Global.asax` application files
- Detect MVC controllers from C# source files
- Detect MVC action methods from C# source files
- Detect MVC route attributes such as `[Route]` and `[RoutePrefix]`
- Detect MVC action, filter, and security-related attributes such as `[HttpGet]`, `[HttpPost]`, `[Authorize]`, `[AllowAnonymous]`, `[ValidateAntiForgeryToken]`, and `[OutputCache]`
- Detect Web API controllers from C# source files
- Detect Web API action methods from C# source files
- Detect Web API route attributes such as `[Route]` and `[RoutePrefix]`
- Detect Web API action, filter, and security-related attributes such as `[HttpGet]`, `[HttpPost]`, `[Authorize]`, and `[AllowAnonymous]`
- Detect MVC area registration classes from C# source files
- Detect route configuration files such as `RouteConfig.cs`
- Detect MVC application startup methods such as `Application_Start`
- Detect MVC startup registration calls such as `AreaRegistration.RegisterAllAreas()`, `RouteConfig.RegisterRoutes(...)`, `BundleConfig.RegisterBundles(...)`, and `FilterConfig.RegisterGlobalFilters(...)`
- Detect Web API configuration files such as `WebApiConfig.cs`
- Detect Web API route registration calls such as `MapHttpRoute(...)`
- Detect Web API startup registration calls such as `GlobalConfiguration.Configure(...)` and `WebApiConfig.Register(...)`
- Detect MVC bundle configuration files such as `BundleConfig.cs`
- Detect MVC filter configuration files such as `FilterConfig.cs`
- Detect ASP.NET MVC dependency resolver registration calls such as `DependencyResolver.SetResolver(...)`
- Detect ASP.NET MVC custom controller factory registration calls such as `ControllerBuilder.Current.SetControllerFactory(...)`
- Detect ASP.NET MVC global filter registrations such as `GlobalFilters.Filters.Add(...)`
- Detect ASP.NET MVC model binder registrations such as `ModelBinders.Binders`
- Detect ASP.NET MVC value provider factory registrations such as `ValueProviderFactories.Factories`
- Detect ASP.NET Web API dependency resolver configuration
- Detect ASP.NET Web API formatter configuration
- Detect ASP.NET Web API message handler registration
- Detect ASP.NET Web API filter registration
- Detect ASP.NET Web API CORS registration
- Report discovered legacy ASP.NET artifacts in the generated Markdown report
- Include discovered legacy ASP.NET artifacts in modernisation hint analysis

Post-MVP ideas:

- Deeper ASP.NET authentication and authorization discovery.
- Deeper HTTP module and handler analysis beyond registration discovery, such as concrete type extraction, pipeline mode distinctions, preconditions, verb/path matching details, and migration mapping guidance.
- Route constraint extraction.
- Concrete filter, formatter, message handler, model binder, and value provider type extraction.
- Application lifecycle event discovery beyond `Application_Start`.

---
