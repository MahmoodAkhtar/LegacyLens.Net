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

### Step 2: Markdown report generation

Status: Implemented

- Generate `output/discovery-report.md`
- Include summary counts
- Include solution summary
- Include solution table
- Include project table
- Include target framework summary
- Include package reference summary
- Include project references
- Include assembly references
- Include package references
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

Status: Implemented with conditional quality gates

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
