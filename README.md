# LegacyLens.NET

<p align="center">
  <img src="assets/legacylens-128x128.png" alt="LegacyLens.NET logo" width="128" height="128" />
</p>

LegacyLens.NET is a static discovery tool for unfamiliar, legacy, and modern .NET codebases.

It helps developers quickly understand the structure, dependencies, legacy technology indicators, configuration concerns, and modernisation review areas in a .NET solution without requiring the target solution to build.

## What it does

LegacyLens.NET scans source files and configuration files to discover useful codebase information, including:

- solutions, projects, target frameworks, project references, assembly references, and NuGet package references
- WCF endpoints, binding details, behaviours, service contracts, and operation contracts
- legacy ASP.NET, ASP.NET MVC, and ASP.NET Web API discovery signals
- configuration files, app settings, connection strings, and custom configuration sections
- evidence-backed modernisation hints and prioritised modernisation review areas
- Mermaid project dependency diagrams in the generated Markdown report
- an MVP-scope upgrade-readiness artifact that produces `upgrade-readiness-report.md` with static, evidence-backed upgrade planning signals
- an MVP-scope upgrade-blockers artifact that produces `upgrade-blockers.md` with static, evidence-backed blocker and migration decision signals
- an MVP-scope external-dependencies artifact that produces `external-dependencies.md` with a static, evidence-backed inventory of possible runtime and build-time dependencies outside the repository

## Quick start

LegacyLens.NET is intended to be used as a standalone command-line discovery utility:

```text
download exe
open terminal
run scan
get markdown report
```

Run:

```bash
legacylens scan <path>
```

Example:

```bash
legacylens scan C:\Repos\LegacyApp
```

By default, the Markdown report is written to:

```text
<scan-path>/output/discovery-report.md
```

For full command usage, see [docs/usage.md](docs/usage.md).

The MVP scope now also includes an optional upgrade-readiness artifact:

```bash
legacylens scan <path> --output-dir ./output --artifacts upgrade-readiness --upgrade-target net8.0
```

`--upgrade-target` is optional. When omitted, the report should use general upgrade-readiness language and avoid claiming compatibility with any specific destination framework.

The MVP scope now also includes an optional upgrade-blockers artifact:

```bash
legacylens scan <path> --output-dir ./output --artifacts upgrade-blockers --upgrade-target net8.0
```

`--upgrade-target` is optional. When omitted, the report should use general upgrade-blocker language and avoid claiming compatibility with any specific destination framework.

The MVP scope now also includes an optional external-dependencies artifact:

```bash
legacylens scan <path> --output-dir ./output --artifacts external-dependencies
```

This report should identify possible databases, HTTP/API URLs, WCF/service endpoints, queues, SMTP/email settings, Redis/cache indicators, file shares, cloud service packages, private package feeds, direct vendor DLL references, and infrastructure-related configuration using static evidence only. It should mask sensitive values and avoid claiming that any external system was contacted, verified, reachable, active in production, or complete.

## Example output

The normal `legacylens scan <path>` output is intentionally concise:

```text
LegacyLens.NET

Scan path: C:\Path\To\LegacyApp
Report: C:\Path\To\LegacyApp\output\discovery-report.md

Summary:
- Solutions discovered: 1
- Projects discovered: 4
- Project references discovered: 4
- Package references discovered: 5
- Assembly references discovered: 2
- WCF endpoints discovered: 3
- WCF service contracts discovered: 1
- WCF behaviours discovered: 2
- Legacy ASP.NET artifacts discovered: 50
- Configuration files discovered: 1
- Modernisation hints discovered: 77

Top review areas:
1. WCF migration
2. Legacy ASP.NET migration
3. Target framework review

Markdown report generated:
C:\Path\To\LegacyApp\output\discovery-report.md
```

For detailed report examples, see [docs/report-output.md](docs/report-output.md).

## Current Status

LegacyLens.NET is currently in late MVP development and is focused on hardening the first usable discovery baseline.

The current MVP already provides a standalone CLI scan command that produces a static Markdown discovery report with solution structure, project dependencies, package and assembly references, WCF configuration, WCF service contracts, selected legacy ASP.NET and ASP.NET MVC/Web API signals, evidence-backed modernisation hints, and a prioritised modernisation review summary. The MVP scope also includes separate static artifacts for upgrade readiness, upgrade blockers, and external dependency inventory.

The current implementation can scan a folder containing .NET solutions and projects and discover:

- `.sln` files
- solution names
- C# project file paths referenced by solution files
- `.csproj` files
- project names
- target frameworks
- project-to-project references
- assembly references from `<Reference />` entries in `.csproj` files
- NuGet package references from SDK-style `<PackageReference />` entries in `.csproj` files
- NuGet package references from legacy `packages.config` files located alongside project files
- WCF endpoints from `app.config` and `web.config` files
- WCF endpoint binding configuration names, behaviour configuration names, security modes, transport credential types, message credential types, timeout settings, message size limits, buffer limits, transfer modes, reader quota settings, and metadata exchange endpoint indicators
- WCF service behaviours from `app.config` and `web.config` files
- WCF endpoint behaviours from `app.config` and `web.config` files
- WCF service metadata settings such as `httpGetEnabled` and `httpsGetEnabled`
- WCF service debug settings such as `includeExceptionDetailInFaults`
- WCF service throttling settings such as `maxConcurrentCalls`, `maxConcurrentSessions`, and `maxConcurrentInstances`
- WCF endpoint `webHttp` behaviour indicators
- configuration files from `app.config` and `web.config`
- `appSettings` entry counts
- `connectionStrings` entry counts
- custom configuration section counts from `configSections`
- WCF service contracts from C# source files
- WCF operations marked with `[OperationContract]`, scoped to the containing `[ServiceContract]` interface
- legacy ASP.NET artifacts from files such as `.aspx`, `.ascx`, `.master`, `.asmx`, `.ashx`, and `Global.asax`
- WebForms pages
- WebForms user controls
- WebForms master pages
- ASMX web services
- ASP.NET HTTP handlers
- custom ASP.NET HTTP module registrations from `system.web/httpModules` and `system.webServer/modules` in `web.config`
- custom ASP.NET HTTP handler registrations from `system.web/httpHandlers` and `system.webServer/handlers` in `web.config`
- `Global.asax` application files
- ASP.NET MVC controllers from C# source files
- ASP.NET MVC action methods from C# source files
- ASP.NET MVC route attributes such as `[Route]` and `[RoutePrefix]`
- ASP.NET MVC action, filter, and security-related attributes such as `[HttpGet]`, `[HttpPost]`, `[Authorize]`, `[AllowAnonymous]`, `[ValidateAntiForgeryToken]`, and `[OutputCache]`
- ASP.NET Web API controllers from C# source files
- ASP.NET Web API action methods from C# source files
- ASP.NET Web API route attributes such as `[Route]` and `[RoutePrefix]`
- ASP.NET Web API action, filter, and security-related attributes such as `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`, `[HttpPatch]`, `[AcceptVerbs]`, `[Authorize]`, and `[AllowAnonymous]`
- ASP.NET MVC area registration classes from C# source files
- ASP.NET route configuration files such as `RouteConfig.cs`
- ASP.NET MVC application startup methods such as `Application_Start`
- ASP.NET MVC startup registration calls such as `AreaRegistration.RegisterAllAreas()`, `RouteConfig.RegisterRoutes(...)`, `BundleConfig.RegisterBundles(...)`, and `FilterConfig.RegisterGlobalFilters(...)`
- ASP.NET Web API configuration files such as `WebApiConfig.cs`
- ASP.NET Web API route registration calls such as `MapHttpRoute(...)`
- ASP.NET Web API startup registration calls such as `GlobalConfiguration.Configure(...)` and `WebApiConfig.Register(...)`
- ASP.NET MVC bundle configuration files such as `BundleConfig.cs`
- ASP.NET MVC filter configuration files such as `FilterConfig.cs`
- ASP.NET MVC dependency resolver registration calls such as `DependencyResolver.SetResolver(...)`
- ASP.NET MVC custom controller factory registration calls such as `ControllerBuilder.Current.SetControllerFactory(...)`
- ASP.NET MVC global filter registrations such as `GlobalFilters.Filters.Add(...)`
- ASP.NET MVC model binder registrations such as `ModelBinders.Binders`
- ASP.NET MVC value provider factory registrations such as `ValueProviderFactories.Factories`
- ASP.NET Web API dependency resolver configuration
- ASP.NET Web API formatter configuration
- ASP.NET Web API message handler registration
- ASP.NET Web API filter registration
- ASP.NET Web API CORS registration
- evidence-backed modernisation hints for legacy target frameworks, WCF usage, selected packages, legacy ASP.NET / `System.Web` usage, discovered legacy ASP.NET artifacts, ASP.NET MVC controllers, ASP.NET MVC actions, ASP.NET MVC route attributes, ASP.NET MVC action attributes, ASP.NET MVC area registrations, ASP.NET route configuration, ASP.NET MVC startup registration, ASP.NET MVC bundle configuration, ASP.NET MVC filter configuration, ASP.NET HTTP module registrations, ASP.NET HTTP handler registrations, ASP.NET Web API controllers, ASP.NET Web API actions, ASP.NET Web API route attributes, ASP.NET Web API action attributes, ASP.NET Web API configuration, ASP.NET Web API route registration, ASP.NET Web API startup registration, higher project coupling, selected WCF binding types, WCF security-related endpoint details, WCF timeout settings, WCF message size and buffer limits, WCF transfer modes, WCF reader quotas, metadata exchange endpoints, WCF service behaviours, WCF endpoint behaviours, WCF metadata publishing settings, WCF debug settings, WCF throttling settings, WCF REST-style `webHttp` endpoint behaviours, and configuration-heavy applications
- modernisation hint evidence metadata, including evidence kind, evidence name, source path, and confidence
- external dependency inventory inputs for `external-dependencies.md`, using static evidence such as connection strings, app settings, WCF endpoints, URL-like values, infrastructure package references, direct assembly references, and private package feed configuration where discoverable
- a prioritised modernisation review summary that groups detailed modernisation hints into higher-level review areas such as WCF migration, legacy ASP.NET migration, routing review, startup and request pipeline review, configuration review, dependency review, target framework review, and project dependency review

### MVP-scope upgrade-readiness artifact

The `upgrade-readiness` capability is an MVP-scope addition. It should produce `upgrade-readiness-report.md` as a separate Markdown artifact focused on static upgrade planning. The report should help a developer understand current project target frameworks, project-level upgrade candidates, possible upgrade concerns, package and assembly reference considerations, and configuration/runtime risks such as `System.Web`, WCF, EF6, `packages.config`, direct assembly references, `Web.config`, or legacy ASP.NET artifacts.

This capability should remain evidence-backed and cautious. It must not claim to build the solution, run the application, restore packages, resolve transitive dependencies, inspect NuGet package assets, automatically migrate code, or guarantee compatibility with `net8.0`, `net10.0`, or any other destination framework.

### MVP-scope upgrade-blockers artifact

The `upgrade-blockers` capability is an MVP-scope addition. It should produce `upgrade-blockers.md` as a separate Markdown artifact focused on visible technical blockers and migration decisions that may complicate upgrading a legacy .NET codebase.

The report should help a developer understand which projects or files contain evidence of possible blockers, why each blocker matters, which decisions the development team needs to make, and which blockers should be reviewed first. Typical blocker areas include `System.Web`, legacy ASP.NET artifacts, WCF / `System.ServiceModel`, EF6 / EDMX data access, `packages.config`, direct DLL or assembly references, custom configuration, binding redirects, and runtime coupling.

This capability should remain evidence-backed and cautious. It must not claim to build the solution, run the application, restore packages, resolve transitive dependencies, inspect NuGet package assets, automatically migrate code, prove that migration is impossible, or guarantee compatibility with `net8.0`, `net10.0`, or any other destination framework. A blocker means “requires review”, not “cannot be upgraded”.

### MVP-scope external-dependencies artifact

The `external-dependencies` capability is an MVP-scope addition. It should produce `external-dependencies.md` as a separate Markdown artifact focused on possible systems, services, infrastructure, files, databases, queues, APIs, package feeds, vendor assemblies, or runtime resources outside the repository.

The report should group evidence such as connection strings, HTTP/API URLs, WCF endpoints, queue-related settings or packages, SMTP/email settings, Redis/cache indicators, file shares, cloud service packages, private NuGet feeds, direct vendor DLL references, and infrastructure-related configuration. It should help a developer identify what needs confirmation before migration, testing, deployment, onboarding, or environment setup.

This capability should remain static, evidence-backed, and security-conscious. It must not claim to connect to external systems, validate credentials, verify reachability, inspect production infrastructure, prove production usage, prove that a dependency is unused, expose secrets, or guarantee completeness. Sensitive values should be masked or redacted.

Package discovery behaviour is covered by tests for `<PackageReference />`, `packages.config`, duplicate package handling, and invalid `packages.config` handling.

It can also generate a Markdown discovery report at the default scan output path:

```text
<scan-path>/output/discovery-report.md
```

The generated report currently includes:

- a summary of discovered solutions, projects, project references, assembly references, package references, WCF endpoints, WCF service contracts, WCF behaviours, and legacy ASP.NET artifacts
- a solution table
- a project table
- a target framework summary showing how many projects use each discovered target framework
- a package reference summary showing how many projects reference each discovered package
- a Mermaid project dependency diagram
- project reference information
- assembly reference information
- package reference information
- WCF endpoint information, including binding configuration, security mode, transport credential type, message credential type, and metadata exchange indicators
- WCF binding detail information, including timeout settings, message size limits, buffer limits, and transfer mode
- WCF reader quota information, including max depth, max string content length, max array length, max bytes per read, and max name table character count
- WCF behaviour information, including service behaviours, endpoint behaviours, metadata publishing flags, debug flags, throttling values, and `webHttp` endpoint behaviour indicators
- WCF service contract and operation information
- legacy ASP.NET artifact information, including file-based artifacts, config-based HTTP module and handler registrations, MVC controllers, MVC actions, MVC route attributes, MVC action attributes, MVC area registrations, route configuration, MVC and Web API startup registration, bundle configuration, filter configuration, dependency resolver setup, controller factory setup, model binder setup, value provider setup, Web API controllers, Web API actions, Web API route attributes, Web API action attributes, Web API configuration, Web API formatter configuration, Web API message handler registration, Web API filter registration, Web API CORS registration, artifact kind, name, and file path
- configuration file information, including `appSettings`, `connectionStrings`, and custom configuration section counts
- a modernisation review summary that ranks higher-level review areas by highest severity, review-area priority, and hint counts
- modernisation hints with severity, area, finding, evidence, confidence, source, and reason, including Legacy ASP.NET hints when `System.Web` assembly references or legacy ASP.NET artifacts are found

The following console output is a representative excerpt. Exact counts, paths, and findings may change as the sample application evolves.

Example console output:

```text
Projects discovered:
- SampleLegacyApp.Contracts
  Target framework: net48
- SampleLegacyApp.Data
  Target framework: net48
  Package reference: Dapper
  Package reference: EntityFramework
  Package reference: Newtonsoft.Json
- SampleLegacyApp.Services
  Target framework: net48
  Project reference: ..\SampleLegacyApp.Contracts\SampleLegacyApp.Contracts.csproj
  Project reference: ..\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj
  Assembly reference: System.ServiceModel
- SampleLegacyApp.Web
  Target framework: net48
  Project reference: ..\SampleLegacyApp.Contracts\SampleLegacyApp.Contracts.csproj
  Project reference: ..\SampleLegacyApp.Services\SampleLegacyApp.Services.csproj
  Package reference: Newtonsoft.Json
  Package reference: System.ServiceModel.Http

WCF endpoints discovered:
- SampleLegacyApp.Services.CustomerService
  Address: mex
  Binding: mexHttpBinding
  Contract: IMetadataExchange
  Config file: C:\Path\To\LegacyLens.Net\samples\SampleLegacyApp\SampleLegacyApp.Web\Web.config
- SampleLegacyApp.Services.CustomerService
  Address:
  Binding: basicHttpBinding
  Contract: SampleLegacyApp.Contracts.ICustomerContract
  Config file: C:\Path\To\LegacyLens.Net\samples\SampleLegacyApp\SampleLegacyApp.Web\Web.config
- SampleLegacyApp.Services.CustomerService
  Address:
  Binding: basicHttpBinding
  Contract: SampleLegacyApp.Contracts.ICustomerService
  Config file: C:\Path\To\LegacyLens.Net\samples\SampleLegacyApp\SampleLegacyApp.Web\Web.config

WCF service contracts discovered:
- ICustomerContract
  Source file: C:\Path\To\LegacyLens.Net\samples\SampleLegacyApp\SampleLegacyApp.Contracts\CustomerContracts.cs
  Operation: GetCustomer

WCF behaviours discovered:
- ServiceBehaviour: CustomerServiceBehaviour
  Service metadata: True
  Service debug: True
  Service throttling: True
- EndpointBehaviour: JsonEndpointBehaviour
  Web HTTP: True

Configuration files discovered:
- C:\Path\To\LegacyLens.Net\samples\SampleLegacyApp\SampleLegacyApp.Web\Web.config
  App settings: 2
  Connection strings: 1
  Custom sections: 1

Legacy ASP.NET artifacts discovered:
- WebFormsPage: Default.aspx
- AsmxWebService: CustomerService.asmx
- HttpHandler: Download.ashx
- GlobalAsax: Global.asax
- MvcController: HomeController
- WebApiController: CustomersApiController
- WebApiConfig: WebApiConfig.cs
- WebApiCorsRegistration: config.EnableCors
- HttpModuleRegistration: IntegratedLegacyModule
- HttpModuleRegistration: LegacyAuthModule
- HttpHandlerRegistration: *.legacy
- HttpHandlerRegistration: IntegratedLegacyHandler

Modernisation hints discovered:
- [Risk] Target Framework: SampleLegacyApp.Contracts targets net48
- [Risk] Target Framework: SampleLegacyApp.Data targets net48
- [Risk] Target Framework: SampleLegacyApp.Services targets net48
- [Risk] Target Framework: SampleLegacyApp.Web targets net48
- [Risk] WCF: 3 WCF endpoint(s) discovered
- [Risk] WCF: 1 WCF service contract(s) discovered
- [Warning] WCF Binding: basicHttpBinding endpoint discovered for SampleLegacyApp.Services.CustomerService contract SampleLegacyApp.Contracts.ICustomerContract
- [Warning] WCF Reader Quotas: SampleLegacyApp.Services.CustomerService has explicit WCF reader quota settings
- [Warning] WCF Transfer Mode: SampleLegacyApp.Services.CustomerService uses WCF transfer mode Streamed
- [Risk] Legacy ASP.NET: Default.aspx is a WebForms page
- [Risk] Legacy ASP.NET: CustomerService.asmx is an ASMX web service
- [Warning] Legacy ASP.NET Web API Pipeline: config.EnableCors enables ASP.NET Web API CORS configuration
- [Warning] Configuration: Web.config contains 1 custom configuration section(s)
- [Info] Configuration: Web.config contains 1 connection string(s)
- [Warning] Legacy ASP.NET Request Pipeline: LegacyAuthModule registers an ASP.NET HTTP module
- [Warning] Legacy ASP.NET Request Pipeline: IntegratedLegacyHandler registers an ASP.NET HTTP handler
- [Warning] Packages: SampleLegacyApp.Data references EntityFramework

Modernisation review summary:
- 1. WCF migration
  Highest severity: Risk
  Risks: 3
  Warnings: 7
  Info: 8
  Summary: 3 risk, 7 warning, 8 info hint(s). Review service boundaries, bindings, security, timeout, payload, metadata, contract, and WCF package usage before choosing a migration approach.
- 2. Legacy ASP.NET migration
  Highest severity: Risk
  Risks: 2
  Warnings: 3
  Info: 8
  Summary: 2 risk, 3 warning, 8 info hint(s). Review classic ASP.NET, System.Web, WebForms, ASMX, handlers, MVC, or Web API usage before planning an ASP.NET Core migration.
- 3. Target framework review
  Highest severity: Risk
  Risks: 4
  Warnings: 0
  Info: 0
  Summary: 4 risk, 0 warning, 0 info hint(s). Review target frameworks to understand upgrade paths, .NET Framework dependencies, and modern .NET migration constraints.
- 4. Startup and request pipeline review
  Highest severity: Warning
  Risks: 0
  Warnings: 24
  Info: 3
  Summary: 0 risk, 24 warning, 3 info hint(s). Review application startup, dependency resolver setup, controller factories, global filters, action attributes, formatters, message handlers, CORS, model binding, value providers, bundling, and cross-cutting request behaviour that may need ASP.NET Core equivalents.
- 5. Configuration review
  Highest severity: Warning
  Risks: 0
  Warnings: 1
  Info: 1
  Summary: 0 risk, 1 warning, 1 info hint(s). Review appSettings, connection strings, and custom configuration sections for runtime behaviour and external dependencies.
- 6. Dependency review
  Highest severity: Warning
  Risks: 0
  Warnings: 1
  Info: 2
  Summary: 0 risk, 1 warning, 2 info hint(s). Review package dependencies that may affect migration, replacement, compatibility, or framework upgrade planning.
- 7. Routing review
  Highest severity: Info
  Risks: 0
  Warnings: 0
  Info: 10
  Summary: 0 risk, 0 warning, 10 info hint(s). Review conventional routes, attribute routes, area routes, and Web API route registrations to preserve URL and client compatibility.

Solutions discovered:
- SampleLegacyApp
  Projects: 4

Markdown report generated: C:\Path\To\LegacyLens.Net\samples\SampleLegacyApp\output\discovery-report.md
```

If no solutions, WCF endpoints, WCF service contracts, WCF behaviours, configuration files, legacy ASP.NET artifacts, or modernisation hints are found, the console output shows:

```text
WCF endpoints discovered:
- None

WCF service contracts discovered:
- None

WCF behaviours discovered:
- None

Configuration files discovered:
- None

Legacy ASP.NET artifacts discovered:
- None

Modernisation hints discovered:
- None

Modernisation review summary:
- None

Solutions discovered:
- None
```

---


## Documentation

- [Usage](docs/usage.md)
- [Report output](docs/report-output.md)
- [Discovery capabilities](docs/discovery-capabilities.md)
- [Architecture](docs/architecture.md)
- [MVP scope](docs/mvp.md)
- [Roadmap](docs/roadmap.md)
- [AI development context](docs/ai-context.md)

## Design Principles

LegacyLens.NET is intended to be:

- static-first
- useful without requiring a successful build
- simple to run from the command line
- focused on practical codebase understanding
- useful for both legacy and modern .NET solutions
- able to generate human-readable reports and diagrams
- honest about what has been discovered from source files and configuration

---

## License

This project is licensed under the Apache License, Version 2.0, January 2004.

See the `LICENSE` file for details.
