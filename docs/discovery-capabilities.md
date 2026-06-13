# Discovery Capabilities

This document captures the detailed discovery capabilities currently described for LegacyLens.NET.


## CLI Artifact Selection Note

The MVP artifact selection capability changes how optional reports are generated from the CLI; it does not add new static discovery signals by itself. `--artifacts` should allow one artifact name, a comma-separated subset of artifact names, or `all`, while the normal `discovery-report.md` is always generated. Discovery and analysis scope remains defined by the specific selected artifacts.

## What LegacyLens.NET Can Do Without Building the Solution

LegacyLens.NET is designed to inspect source files directly.

Even if the solution does not build, it can still discover useful information from files such as:

- `.sln`
- solution-level project membership
- `.csproj`
- `packages.config`
- `app.config`
- `web.config`
- configuration file structure
- `appSettings` entries
- `connectionStrings` entries
- custom configuration sections
- C# source files
- source-defined C# types such as classes, interfaces, records, structs, and enums
- source-level class/type dependencies such as constructor parameters, fields, properties, method parameters, return types, local variables, object creation, static member access, base classes, interface implementations, attributes, and generic type arguments
- `.aspx` WebForms pages
- `.ascx` WebForms user controls
- `.master` WebForms master pages
- `.asmx` ASMX web services
- `.ashx` ASP.NET HTTP handlers
- `Global.asax` application files
- custom ASP.NET HTTP module registrations from `system.web/httpModules` and `system.webServer/modules` in `web.config`
- custom ASP.NET HTTP handler registrations from `system.web/httpHandlers` and `system.webServer/handlers` in `web.config`
- ASP.NET MVC controller classes inheriting from `Controller` or `System.Web.Mvc.Controller`
- ASP.NET MVC action methods returning common MVC result types such as `ActionResult`, `ViewResult`, `JsonResult`, `PartialViewResult`, `RedirectResult`, `RedirectToRouteResult`, `FileResult`, `ContentResult`, and `HttpStatusCodeResult`
- ASP.NET MVC route attributes such as `[Route]` and `[RoutePrefix]`
- ASP.NET MVC action, filter, and security-related attributes such as `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`, `[HttpPatch]`, `[AcceptVerbs]`, `[Authorize]`, `[AllowAnonymous]`, `[ValidateAntiForgeryToken]`, and `[OutputCache]`
- ASP.NET Web API controller classes inheriting from `ApiController` or `System.Web.Http.ApiController`
- ASP.NET Web API action methods returning common Web API result types such as `IHttpActionResult` and `HttpResponseMessage`
- ASP.NET Web API route attributes such as `[Route]` and `[RoutePrefix]`
- ASP.NET Web API action, filter, and security-related attributes such as `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`, `[HttpPatch]`, `[AcceptVerbs]`, `[Authorize]`, and `[AllowAnonymous]`
- ASP.NET MVC area registration classes inheriting from `AreaRegistration` or `System.Web.Mvc.AreaRegistration`
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
- WCF configuration files
- WCF endpoint binding configuration names
- WCF endpoint security modes and credential types
- WCF endpoint timeout settings
- WCF endpoint message size and buffer limits
- WCF endpoint transfer modes
- WCF endpoint reader quota settings
- WCF metadata exchange endpoint indicators
- WCF service behaviours from configuration files
- WCF endpoint behaviours from configuration files
- WCF service metadata settings from `<serviceMetadata>`
- WCF service debug settings from `<serviceDebug>`
- WCF service throttling settings from `<serviceThrottling>`
- WCF endpoint `webHttp` behaviour indicators
- WCF `[ServiceContract]` interfaces
- WCF `[OperationContract]` methods scoped to their containing service contract interface
- project references
- assembly references
- package references
- package compatibility review metadata for upgrade planning
- prioritised modernisation review areas derived from discovered hints
- upgrade-readiness analysis inputs for `upgrade-readiness-report.md`, using existing static evidence such as project targets, packages, assembly references, WCF, legacy ASP.NET artifacts, and configuration indicators
- upgrade-blockers analysis inputs for `upgrade-blockers.md`, using existing static evidence such as `System.Web`, legacy ASP.NET artifacts, WCF/System.ServiceModel, EF6/EDMX/data-access indicators where available, `packages.config`, assembly references, direct DLL or `HintPath` references where available, configuration indicators, and existing modernisation/package review findings
- external-dependencies analysis inputs for `external-dependencies.md`, using static evidence such as connection strings, app settings, URL-like values, WCF endpoints, messaging/cache/email/cloud package indicators, UNC or local path values, private NuGet feeds, and direct assembly or vendor DLL references where discoverable
- data-access analysis inputs for `data-access-inventory.md`, using static evidence such as connection strings, provider names, database packages, database assembly references, EF6, EF Core, EDMX files, EF T4 templates, LINQ to SQL `.dbml` files, ADO.NET indicators, Dapper indicators, NHibernate indicators, raw SQL strings where feasible, stored procedure indicators where feasible, repository and unit-of-work class names, and migration folders where discoverable
- configuration-inventory analysis inputs for `configuration-inventory.md`, using static evidence such as configuration files, app settings, connection strings, custom configuration sections, environment transforms, WCF `system.serviceModel` sections, ASP.NET/IIS `system.web` and `system.webServer` sections, binding redirects, authentication and authorization settings, logging/diagnostics sections, Entity Framework configuration, SMTP/mail settings, and configuration API usage such as `ConfigurationManager.AppSettings`, `ConfigurationManager.ConnectionStrings`, `IConfiguration`, and `GetSection` where discoverable
- edmx-analysis inputs for `edmx-analysis.md`, using static EDMX XML evidence such as CSDL conceptual entities, entity sets, keys, associations, navigation properties, complex types, function imports, SSDL storage entity sets, tables, views, columns, store functions, defining queries, MSL entity mappings, scalar property mappings, association mappings, function import mappings, modification function mappings, query views, designer metadata, and companion T4/generated files where discoverable
- class-dependencies inputs for `class-dependencies.md`, using static C# source evidence such as source-defined types, constructor parameters, fields, properties, method parameters, return types, local variables, object creation, static member access, inheritance, interface implementations, attributes, generic type usage, coupling hotspots, hardcoded concrete dependencies, and static dependency concerns where discoverable

This makes it useful for old or broken solutions where restoring packages, installing SDKs, or compiling the code may not be possible immediately.


> Note: upgrade-readiness is now MVP scope as a separate static report artifact. It should produce `upgrade-readiness-report.md` and should use existing discovered evidence to classify project-level readiness and possible upgrade concerns. It should not claim to build the solution, restore packages, resolve transitive dependencies, inspect package assets, automatically migrate code, or guarantee compatibility with a destination framework.

> Note: upgrade-blockers is now MVP scope as a separate static report artifact. It should produce `upgrade-blockers.md` and should use existing discovered evidence to identify visible technical blockers, migration decisions, and higher-risk areas that may complicate upgrade planning. It should not claim to build the solution, restore packages, resolve transitive dependencies, inspect package assets, prove migration is impossible, automatically migrate code, or guarantee compatibility with a destination framework.

> Note: external-dependencies is now MVP scope as a separate static report artifact. It should produce `external-dependencies.md` and should use existing discovered evidence to identify possible runtime and build-time dependencies outside the repository. It should not claim to connect to external systems, validate credentials, verify reachability, inspect production infrastructure, prove production usage, prove that a dependency is unused, expose secrets, or guarantee completeness.

> Note: configuration-inventory is now MVP scope as a separate static report artifact. It should produce `configuration-inventory.md` and should use static repository evidence to identify visible configuration files, sections, settings, transforms, and migration-relevant configuration concerns. It should not claim to run the application, apply transforms, validate credentials, connect to external systems, prove production usage, prove settings are used or unused, fully evaluate runtime configuration inheritance, resolve deployment-time substitutions, expose secrets, or guarantee completeness. Sensitive values should be masked or redacted.

> Note: data-access is now MVP scope as a separate static report artifact. It should produce `data-access-inventory.md` and should use existing discovered evidence to identify visible data access technologies, patterns, and migration concerns. It should not claim to connect to databases, validate credentials or connection strings, execute SQL, inspect schemas, run migrations, scaffold EF Core models, reverse-engineer databases, prove runtime usage, prove a query or stored procedure is unused, automatically migrate data access code, or guarantee compatibility.

> Note: edmx-analysis is now MVP scope as a separate static report artifact. It should produce `edmx-analysis.md` and should use static EDMX XML evidence to identify conceptual model, storage model, mapping model, designer metadata, companion generated file, and EF Core migration concern signals. It should not claim to connect to a database, validate EDMX mappings against a live schema, generate EF Core models, convert EDMX to EF Core, build the solution, run NuGet restore, guarantee compatibility, fully understand custom T4 templates, or treat all EF Core equivalents as direct one-to-one replacements.

> Note: class-dependencies is now MVP scope as a separate static report artifact. It should produce `class-dependencies.md` and should use static C# source evidence to identify source-level type relationships, coupling hotspots, hardcoded concrete dependencies, static dependency concerns, and focused Mermaid diagrams with dependency-kind edge labels. It should not claim to build the solution, restore packages, resolve runtime dependency injection, execute code, understand reflection or dynamic loading, fully understand generated code, prove runtime usage, or produce a runtime call graph.

> Note: package reference discovery currently supports both SDK-style `<PackageReference />` entries in `.csproj` files and legacy `packages.config` files located alongside project files. Invalid or unreadable `packages.config` files are ignored so discovery can continue.

> Note: package compatibility review is now MVP scope. It should remain static and evidence-backed: package name, version where available, package source format, project target framework, package target framework where available, source path, and possible compatibility concerns. It should not claim to perform NuGet restore, transitive dependency resolution, package asset inspection, or guaranteed compatibility checks against a destination framework.

> Note: assembly reference discovery currently supports `<Reference Include="..." />` entries in `.csproj` files. Version metadata is removed so references such as `System.Web.Mvc, Version=5.2.9.0` are reported as `System.Web.Mvc`.

> Note: configuration file discovery currently supports `app.config` and `web.config` files. Invalid or unreadable configuration files are ignored so discovery can continue.

> Note: WCF endpoint discovery currently reads configured service endpoints from `app.config` and `web.config` files. Where endpoints reference named binding configurations, LegacyLens.NET also attempts to resolve related security mode, transport credential type, message credential type, timeout, message size, buffer, transfer mode, and reader quota details from the matching binding configuration.

> Note: WCF behaviour discovery currently reads selected service behaviour and endpoint behaviour settings from `app.config` and `web.config` files, including service metadata, service debug, service throttling, and endpoint `webHttp` behaviour indicators. The codebase uses the British spelling `Behaviour` for model and report names, while WCF XML uses the standard WCF element names `<behaviors>`, `<serviceBehaviors>`, `<endpointBehaviors>`, and `<behavior>`.

> Note: legacy ASP.NET artifact discovery currently detects file-based classic ASP.NET artifacts such as `.aspx`, `.ascx`, `.master`, `.asmx`, `.ashx`, and `Global.asax`, selected config-based ASP.NET pipeline registrations such as HTTP modules and HTTP handlers from `web.config`, and selected source-level ASP.NET MVC and Web API indicators such as MVC controllers, MVC action methods, MVC route attributes, MVC action attributes, MVC area registrations, Web API controllers, Web API action methods, Web API route attributes, Web API action attributes, `RouteConfig.cs`, `WebApiConfig.cs`, `Application_Start`, MVC startup registration calls, Web API startup registration calls, `BundleConfig.cs`, and `FilterConfig.cs`. These are static discovery signals and do not require the application to build or run.

> Note: HTTP module and handler discovery currently identifies registrations from `web.config`. It does not yet perform deeper analysis of the referenced module or handler implementation type, pipeline mode, preconditions, or migration mapping. Those details remain post-MVP unless needed to fix a clear report-quality issue.

> Note: Legacy ASP.NET and WCF source-level discovery is intentionally static and heuristic. The MVP aims to identify high-value migration signals, not to provide a complete semantic analysis of every possible framework usage pattern.

> Note: modernisation review summary generation groups the detailed modernisation hints into higher-level review areas. Review areas are ranked using highest discovered severity, a lightweight review-area priority, and hint counts. This is intended to help developers quickly identify where to look first while still preserving the detailed hint table as supporting evidence. The priority weighting prevents generic baseline findings, such as multiple projects targeting .NET Framework, from always outranking more actionable migration areas such as WCF or Legacy ASP.NET when they have the same highest severity.

> Note: modernisation hints include evidence metadata where a clear source can be identified. Evidence may point to a project, package reference, assembly reference, WCF endpoint, WCF service contract, WCF behaviour, legacy ASP.NET artifact, configuration file, or analysis summary. The generated report includes the evidence kind, evidence name, confidence, source path where available, and the reason for the hint. Legacy ASP.NET artifact evidence prefers the most specific matching artifact name so, for example, an action attribute hint can point to `HomeController.Index [HttpGet]` rather than only `HomeController`.

> Note: solution discovery currently supports `.sln` files and extracts referenced C# project paths from project entries. Non-C# project entries and solution folders are ignored.

---

## Class Dependency Discovery

The `class-dependencies` capability should analyse C# source files under discovered projects and identify source-level relationships between source-defined types without building the solution.

Current MVP discovery expectations include:

- finding `.cs` files under discovered project folders
- excluding build output paths such as `bin` and `obj`
- discovering source-defined types such as classes, interfaces, records, structs, and enums where useful
- focusing the MVP report mainly on classes and their dependencies
- detecting dependencies created by constructor parameters, field types, property types, method parameters, method return types, local variable declarations, object creation, static member access, base classes, interface implementations, attribute usage, and generic type arguments
- tracking project name, source path, line number, source type, target type, dependency kind, and concise source evidence where possible
- classifying dependency kinds with friendly report labels such as `constructor parameter`, `field`, `property`, `method parameter`, `return type`, `local variable`, `hardcoded new`, `static access`, `inherits`, `implements`, `attribute`, and `generic type`
- identifying coupling concerns from the dependency inventory, including hardcoded concrete dependencies, direct infrastructure construction, static access, concrete field/property dependencies, constructor parameters to concrete classes, inheritance from concrete base classes, framework-specific attributes, and time access such as `DateTime.Now` or `DateTime.UtcNow`
- assigning cautious concern severity such as `High`, `Medium`, or `Low`
- producing high-coupling hotspot summaries using outgoing dependency count, incoming dependency count, and concern count
- producing a focused Mermaid diagram that labels edges by dependency kind and groups multiple dependency kinds between the same source and target where practical

The generated `class-dependencies.md` report should include summary counts, top coupled types, coupling concerns, hardcoded concrete dependencies, static dependency hotspots, a focused dependency diagram, a full type dependency inventory, type-level detail sections, and notes/limitations.

This capability should use cautious wording such as `source-level dependency`, `possible coupling concern`, `suggested review`, `evidence`, and `static analysis finding`. It should avoid overclaiming runtime behaviour.

Out of scope for MVP:

- MSBuild compilation
- NuGet restore
- runtime dependency injection resolution
- reflection and dynamic loading analysis
- generated code behaviour guarantees
- conditional runtime behaviour analysis
- runtime call graphs
- proving that a dependency is always used at runtime
- proving that a dependency is unused

---

## EDMX Analysis Discovery

The `edmx-analysis` capability should discover Entity Framework `.edmx` files used by legacy EF6 Database First or Model First projects and inspect their XML structure without building the solution or connecting to a database.

Current MVP discovery expectations include:

- finding `.edmx` files under scanned project folders
- associating each EDMX file with the nearest discovered project where possible
- parsing EDMX XML defensively with `System.Xml.Linq`
- supporting common EDMX, CSDL, SSDL, and MSL XML namespace versions by using namespace-agnostic local-name matching where practical
- identifying whether CSDL conceptual model, SSDL storage model, MSL mapping model, and designer metadata sections are present
- extracting conceptual model evidence such as schemas, entity types, entity sets, key properties, properties, associations, association sets, navigation properties, complex types, and function imports
- extracting storage model evidence such as schemas, entity types, entity sets, table/view names, columns, keys, associations, store functions, and defining queries
- extracting mapping model evidence such as entity set mappings, entity type mappings, mapping fragments, scalar property mappings, association set mappings, function import mappings, modification function mappings, and query views
- detecting companion generated files such as `.tt`, `.Designer.cs`, and generated context/model files near the EDMX file
- producing upgrade concerns from concrete static evidence, such as EDMX usage, stored procedure/function mappings, query-backed entities, complex types, designer metadata, and companion generated files

The generated `edmx-analysis.md` report should include summary counts, an EDMX files table, upgrade concerns, conceptual model details, storage model details, associations, function imports and store functions, mapping details, companion generated files, and clear notes/limitations.

This capability is more focused than the broader `data-access` inventory. The data-access report can identify that EDMX files exist as data-access indicators; the edmx-analysis report should inspect the EDMX file contents in more detail.


---

## Solution Discovery

LegacyLens.NET can discover Visual Studio solution files and the C# projects referenced by them.

Current solution discovery supports:

- finding `.sln` files under the scanned folder
- reading the solution name from the `.sln` file name
- extracting referenced `.csproj` paths from solution project entries
- resolving project paths relative to the solution file location
- ignoring solution folders and non-C# project entries
- removing duplicate project paths case-insensitively

Example solution project entry:

```text
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "SampleLegacyApp.Web", "SampleLegacyApp.Web\SampleLegacyApp.Web.csproj", "{11111111-1111-1111-1111-111111111111}"
EndProject
```

Example report output:

```markdown
## Solutions

| Solution | Projects | Solution File |
|---|---:|---|
| SampleLegacyApp | 4 | `...\SampleLegacyApp.sln` |
```

This helps identify the solution-level structure of a codebase before looking at individual project dependencies.

---

## Package Reference Discovery

LegacyLens.NET can discover NuGet package references from both modern and legacy project styles.

Current package discovery supports:

- SDK-style `<PackageReference />` entries inside `.csproj` files
- SDK-style package versions from `Version` attributes where available
- SDK-style package versions from nested `<Version>` elements where available
- legacy `packages.config` files located in the same folder as the project file
- legacy `packages.config` package versions from the `version` attribute
- legacy `packages.config` package target frameworks from the `targetFramework` attribute
- package source format identification, such as `PackageReference` or `packages.config`
- source path tracking so the report can point back to the `.csproj` or `packages.config` file that supplied the package evidence

Example SDK-style package reference:

```xml
<ItemGroup>
  <PackageReference Include="Dapper" Version="2.1.66" />
</ItemGroup>
```

Example legacy `packages.config` file:

```xml
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="EntityFramework" version="6.4.4" targetFramework="net48" />
  <package id="Newtonsoft.Json" version="13.0.3" targetFramework="net48" />
</packages>
```

Package names discovered from both sources are merged into the project package reference list. Duplicate package names are removed case-insensitively for summary purposes, while the richer package compatibility review should preserve useful metadata such as version, package target framework, source format, and source path where available.

This helps LegacyLens.NET identify important legacy dependencies even when older .NET Framework projects do not use SDK-style package references.

### Package Compatibility Review

Package compatibility review is an MVP-scope extension of package reference discovery.

The purpose is to help a developer preparing an upgrade see which packages may need attention before choosing an upgrade path. The review should use static evidence only and should be worded as possible compatibility concerns, not definitive migration advice.

The package compatibility review should report:

| Field | Description |
|---|---|
| Project | Project containing the package reference |
| Project target framework | Target framework or target frameworks declared by the project |
| Package | NuGet package id |
| Version | Package version where available |
| Package target framework | `packages.config` target framework where available |
| Source | `PackageReference` or `packages.config` |
| Source path | `.csproj` or `packages.config` file path |
| Concern | Static compatibility concern, or a clear indication that no specific concern was detected by the MVP rules |

Initial MVP concern rules should include:

- `System.ServiceModel.*` packages as WCF-related upgrade planning concerns
- `EntityFramework` as classic Entity Framework review before EF Core or modern .NET migration
- `Newtonsoft.Json` as an informational serialization behaviour review item
- packages with missing versions as package restore and upgrade planning concerns
- `packages.config` package target frameworks that differ from the project target framework as review concerns
- packages tied to old `.NET Framework` project targets as dependency review inputs

Example report output:

```markdown
## Package Compatibility Review

| Project | Project Target Framework | Package | Version | Package Target Framework | Source | Concern |
|---|---|---|---|---|---|---|
| SampleLegacyApp.Data | net48 | EntityFramework | 6.4.4 | net48 | packages.config | Classic Entity Framework should be reviewed before migration to EF Core or modern .NET. |
| SampleLegacyApp.Web | net48 | System.ServiceModel.Http | unknown |  | PackageReference | WCF-related package. Review WCF usage and replacement strategy before upgrading. |
```

The review should not claim to know whether a package is compatible with .NET 8, .NET 9, .NET 10, or any other destination framework unless future implementation adds package metadata lookup or package asset inspection.

---

## Assembly Reference Discovery

LegacyLens.NET can discover framework assembly references from `.csproj` files.

This is useful for older .NET Framework projects where important dependencies may appear as assembly references rather than NuGet package references.

Current assembly reference discovery supports:

- `<Reference Include="..." />` entries inside `.csproj` files
- assembly reference names with version metadata, normalised to the assembly name
- duplicate assembly references removed case-insensitively

Example assembly references:

```xml
<ItemGroup>
  <Reference Include="System.Web" />
  <Reference Include="System.Web.Mvc, Version=5.2.9.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" />
</ItemGroup>
```

These are discovered as:

```text
System.Web
System.Web.Mvc
```

This helps LegacyLens.NET identify legacy ASP.NET indicators that may not appear as NuGet package references.

---

## Configuration File Discovery

LegacyLens.NET can discover useful configuration information from `app.config` and `web.config` files.

This is useful for legacy .NET Framework applications where important behaviour, environment-specific settings, and external dependencies may be defined in configuration rather than code.

Current configuration discovery supports:

- counting `appSettings` entries
- counting `connectionStrings` entries
- counting custom configuration sections from `configSections`
- ignoring invalid or unreadable configuration files so discovery can continue

Example configuration:

```xml
<configuration>
  <configSections>
    <section name="customSettings" type="Legacy.CustomSettingsSection, Legacy" />
  </configSections>

  <appSettings>
    <add key="FeatureToggle" value="true" />
    <add key="LegacyMode" value="enabled" />
  </appSettings>

  <connectionStrings>
    <add name="MainDatabase" connectionString="Server=.;Database=Legacy;" />
  </connectionStrings>
</configuration>
```

These values are reported in the generated Markdown report:

```markdown
## Configuration Files

| Config File | App Settings | Connection Strings | Custom Sections |
|---|---:|---:|---:|
| `...\SampleLegacyApp.Web\Web.config` | 2 | 1 | 1 |
```

The configuration file summary is intentionally separate from config-based ASP.NET artifact discovery. For example, `appSettings`, `connectionStrings`, and `configSections` are counted in the `Configuration Files` section, while HTTP module and handler registrations from `web.config` are reported as legacy ASP.NET artifacts because they affect the ASP.NET request pipeline.

This helps identify applications where important runtime behaviour or migration concerns may be hidden in configuration files.

---

## Legacy ASP.NET Artifact Discovery

LegacyLens.NET can detect selected classic ASP.NET artifacts from source folders without building or running the application.

This is useful for older .NET Framework web applications where important migration work may be hidden in WebForms pages, user controls, master pages, ASMX services, custom handlers, config-based HTTP module and handler registrations, MVC controllers, MVC area registrations, Web API controllers, route configuration, Web API configuration, MVC startup registration, Web API startup registration, bundle configuration, filter configuration, or application lifecycle files.

Current legacy ASP.NET artifact discovery supports:

- `.aspx` WebForms pages
- `.ascx` WebForms user controls
- `.master` WebForms master pages
- `.asmx` ASMX web services
- `.ashx` ASP.NET HTTP handlers
- `Global.asax` application files
- custom ASP.NET HTTP module registrations from `system.web/httpModules` and `system.webServer/modules` in `web.config`
- custom ASP.NET HTTP handler registrations from `system.web/httpHandlers` and `system.webServer/handlers` in `web.config`
- ASP.NET MVC controllers from C# source files
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

Example files:

```text
SampleLegacyApp.Web/
├── App_Start/
│   ├── BundleConfig.cs
│   ├── FilterConfig.cs
│   ├── RouteConfig.cs
│   └── WebApiConfig.cs
├── Areas/
│   └── Admin/
│       └── AdminAreaRegistration.cs
├── Controllers/
│   ├── HomeController.cs
│   └── CustomersApiController.cs
├── Default.aspx
├── CustomerSummary.ascx
├── Site.master
├── CustomerService.asmx
├── Download.ashx
├── Global.asax
└── Global.asax.cs
```

Example `web.config` HTTP module and handler registrations:

```xml
<configuration>
  <system.web>
    <httpModules>
      <add name="LegacyAuthModule"
           type="SampleLegacyApp.Web.LegacyAuthModule, SampleLegacyApp.Web" />
    </httpModules>

    <httpHandlers>
      <add path="*.legacy"
           verb="*"
           type="SampleLegacyApp.Web.LegacyHandler, SampleLegacyApp.Web" />
    </httpHandlers>
  </system.web>

  <system.webServer>
    <modules>
      <add name="IntegratedLegacyModule"
           type="SampleLegacyApp.Web.IntegratedLegacyModule, SampleLegacyApp.Web" />
    </modules>

    <handlers>
      <add name="IntegratedLegacyHandler"
           path="legacy.axd"
           verb="*"
           type="SampleLegacyApp.Web.IntegratedLegacyHandler, SampleLegacyApp.Web" />
    </handlers>
  </system.webServer>
</configuration>
```

Example MVC controller:

```csharp
using System.Web.Mvc;

namespace SampleLegacyApp.Web.Controllers;

[RoutePrefix("home")]
public class HomeController : Controller
{
    [HttpGet]
    [Route("")]
    public ActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("save")]
    public ActionResult Save()
    {
        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    [Route("summary")]
    public JsonResult Summary()
    {
        return Json(
            new
            {
                Message = "Sample legacy MVC JSON endpoint"
            },
            JsonRequestBehavior.AllowGet);
    }
}
```

Example Web API controller:

```csharp
using System.Web.Http;

namespace SampleLegacyApp.Web.Controllers;

[RoutePrefix("api/customers")]
public class CustomersApiController : ApiController
{
    [HttpGet]
    [Route("{id}")]
    public IHttpActionResult Get(int id)
    {
        return Ok(new
        {
            Id = id,
            Name = "Sample customer"
        });
    }

    [HttpPost]
    [Route("")]
    public IHttpActionResult Create(CustomerRequest request)
    {
        return Ok(new
        {
            request.Name
        });
    }
}

public sealed class CustomerRequest
{
    public string? Name { get; init; }
}
```

Example MVC area registration:

```csharp
using System.Web.Mvc;

namespace SampleLegacyApp.Web.Areas.Admin;

public class AdminAreaRegistration : AreaRegistration
{
    public override string AreaName => "Admin";

    public override void RegisterArea(AreaRegistrationContext context)
    {
        context.MapRoute(
            name: "Admin_default",
            url: "Admin/{controller}/{action}/{id}",
            defaults: new
            {
                action = "Index",
                id = UrlParameter.Optional
            });
    }
}
```

Example route configuration:

```csharp
using System.Web.Mvc;
using System.Web.Routing;

namespace SampleLegacyApp.Web;

public static class RouteConfig
{
    public static void RegisterRoutes(RouteCollection routes)
    {
        routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

        routes.MapRoute(
            name: "Default",
            url: "{controller}/{action}/{id}",
            defaults: new
            {
                controller = "Home",
                action = "Index",
                id = UrlParameter.Optional
            });
    }
}
```

Example Web API configuration:

```csharp
using System.Web.Http;

namespace SampleLegacyApp.Web;

public static class WebApiConfig
{
    public static void Register(HttpConfiguration config)
    {
        config.MapHttpAttributeRoutes();

        config.Routes.MapHttpRoute(
            name: "DefaultApi",
            routeTemplate: "api/{controller}/{id}",
            defaults: new
            {
                id = RouteParameter.Optional
            });
    }
}
```

Example MVC and Web API application startup:

```csharp
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;

namespace SampleLegacyApp.Web;

public class MvcApplication : HttpApplication
{
    protected void Application_Start()
    {
        AreaRegistration.RegisterAllAreas();
        GlobalConfiguration.Configure(WebApiConfig.Register);
        FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
        RouteConfig.RegisterRoutes(RouteTable.Routes);
        BundleConfig.RegisterBundles(null);
    }
}
```

Example bundle configuration:

```csharp
namespace SampleLegacyApp.Web;

public static class BundleConfig
{
    public static void RegisterBundles(object? bundles)
    {
    }
}
```

Example filter configuration:

```csharp
using System.Web.Mvc;

namespace SampleLegacyApp.Web;

public static class FilterConfig
{
    public static void RegisterGlobalFilters(GlobalFilterCollection filters)
    {
        filters.Add(new HandleErrorAttribute());
    }
}
```

These are reported in the generated Markdown report:

```markdown
## Legacy ASP.NET Artifacts

| Kind | Name | File |
|---|---|---|
| WebFormsPage | Default.aspx | `...\SampleLegacyApp.Web\Default.aspx` |
| AsmxWebService | CustomerService.asmx | `...\SampleLegacyApp.Web\CustomerService.asmx` |
| HttpHandler | Download.ashx | `...\SampleLegacyApp.Web\Download.ashx` |
| GlobalAsax | Global.asax | `...\SampleLegacyApp.Web\Global.asax` |
| MvcController | HomeController | `...\SampleLegacyApp.Web\Controllers\HomeController.cs` |
| MvcAction | HomeController.Index | `...\SampleLegacyApp.Web\Controllers\HomeController.cs` |
| MvcAction | HomeController.Save | `...\SampleLegacyApp.Web\Controllers\HomeController.cs` |
| MvcAction | HomeController.Summary | `...\SampleLegacyApp.Web\Controllers\HomeController.cs` |
| MvcRouteAttribute | HomeController [RoutePrefix] | `...\SampleLegacyApp.Web\Controllers\HomeController.cs` |
| MvcRouteAttribute | HomeController.Index [Route] | `...\SampleLegacyApp.Web\Controllers\HomeController.cs` |
| MvcRouteAttribute | HomeController.Save [Route] | `...\SampleLegacyApp.Web\Controllers\HomeController.cs` |
| MvcRouteAttribute | HomeController.Summary [Route] | `...\SampleLegacyApp.Web\Controllers\HomeController.cs` |
| MvcActionAttribute | HomeController.Index [HttpGet] | `...\SampleLegacyApp.Web\Controllers\HomeController.cs` |
| MvcActionAttribute | HomeController.Save [HttpPost] | `...\SampleLegacyApp.Web\Controllers\HomeController.cs` |
| MvcActionAttribute | HomeController.Save [ValidateAntiForgeryToken] | `...\SampleLegacyApp.Web\Controllers\HomeController.cs` |
| MvcActionAttribute | HomeController.Summary [AllowAnonymous] | `...\SampleLegacyApp.Web\Controllers\HomeController.cs` |
| WebApiController | CustomersApiController | `...\SampleLegacyApp.Web\Controllers\CustomersApiController.cs` |
| WebApiAction | CustomersApiController.Get | `...\SampleLegacyApp.Web\Controllers\CustomersApiController.cs` |
| WebApiAction | CustomersApiController.Create | `...\SampleLegacyApp.Web\Controllers\CustomersApiController.cs` |
| WebApiRouteAttribute | CustomersApiController [RoutePrefix] | `...\SampleLegacyApp.Web\Controllers\CustomersApiController.cs` |
| WebApiRouteAttribute | CustomersApiController.Get [Route] | `...\SampleLegacyApp.Web\Controllers\CustomersApiController.cs` |
| WebApiRouteAttribute | CustomersApiController.Create [Route] | `...\SampleLegacyApp.Web\Controllers\CustomersApiController.cs` |
| WebApiActionAttribute | CustomersApiController.Get [HttpGet] | `...\SampleLegacyApp.Web\Controllers\CustomersApiController.cs` |
| WebApiActionAttribute | CustomersApiController.Create [HttpPost] | `...\SampleLegacyApp.Web\Controllers\CustomersApiController.cs` |
| RouteConfig | RouteConfig.cs | `...\SampleLegacyApp.Web\App_Start\RouteConfig.cs` |
| WebApiConfig | WebApiConfig.cs | `...\SampleLegacyApp.Web\App_Start\WebApiConfig.cs` |
| WebApiRouteRegistrationCall | MapHttpRoute | `...\SampleLegacyApp.Web\App_Start\WebApiConfig.cs` |
| WebApiGlobalConfigurationCall | GlobalConfiguration.Configure | `...\SampleLegacyApp.Web\Global.asax.cs` |
| AreaRegistration | AdminAreaRegistration | `...\SampleLegacyApp.Web\Areas\Admin\AdminAreaRegistration.cs` |
| MvcApplicationStartup | Global.asax.cs Application_Start | `...\SampleLegacyApp.Web\Global.asax.cs` |
| MvcAreaRegistrationCall | AreaRegistration.RegisterAllAreas | `...\SampleLegacyApp.Web\Global.asax.cs` |
| MvcRouteRegistrationCall | RouteConfig.RegisterRoutes | `...\SampleLegacyApp.Web\Global.asax.cs` |
| MvcBundleRegistrationCall | BundleConfig.RegisterBundles | `...\SampleLegacyApp.Web\Global.asax.cs` |
| MvcFilterRegistrationCall | FilterConfig.RegisterGlobalFilters | `...\SampleLegacyApp.Web\Global.asax.cs` |
| MvcBundleConfig | BundleConfig.cs | `...\SampleLegacyApp.Web\App_Start\BundleConfig.cs` |
| MvcFilterConfig | FilterConfig.cs | `...\SampleLegacyApp.Web\App_Start\FilterConfig.cs` |
| HttpModuleRegistration | IntegratedLegacyModule | `...\SampleLegacyApp.Web\Web.config` |
| HttpModuleRegistration | LegacyAuthModule | `...\SampleLegacyApp.Web\Web.config` |
| HttpHandlerRegistration | *.legacy | `...\SampleLegacyApp.Web\Web.config` |
| HttpHandlerRegistration | IntegratedLegacyHandler | `...\SampleLegacyApp.Web\Web.config` |
```

HTTP module and handler registrations discovered from `web.config` are treated as warning-level request pipeline review items because they can affect authentication, authorization, logging, headers, errors, custom routing, file handling, or other request lifecycle behaviour that may need explicit ASP.NET Core middleware, endpoint, or controller equivalents.

These artifacts are also used as modernisation hint inputs. For example, WebForms pages and ASMX web services are treated as higher-risk migration indicators because they usually need redesign, replacement, or compatibility planning when moving to modern ASP.NET. MVC controllers are treated as warning-level review items because they may contain routing, action filters, model binding, authentication, or `System.Web`-specific behaviour. MVC action methods are treated as informational review items because they identify request-handling behaviour that may need controller, endpoint, result-shape, model-binding, or filter review during migration. MVC route attributes are treated as informational routing review items because they may define URL patterns that need mapping to ASP.NET Core endpoint routing. MVC action attributes are treated as warning-level review items because HTTP verb, authorization, anonymous access, anti-forgery, output caching, and related attributes can materially affect migrated endpoint behaviour. MVC area registrations and route configuration files are treated as informational review items because they may define area-specific routes, URL patterns, defaults, constraints, ignored routes, or feature boundaries that need mapping to ASP.NET Core endpoint routing. MVC application startup methods and startup registration calls are treated as informational review items because they show where classic ASP.NET MVC routing, areas, filters, bundles, dependency injection, error handling, or lifecycle behaviour may be wired into the application. Bundle configuration and bundle registration are treated as warning-level review items because ASP.NET MVC bundling and minification usually need replacement with a modern static asset, build, or bundling strategy. Filter configuration and global filter registration are treated as warning-level review items because global filters can affect authorization, error handling, caching, model binding, and other cross-cutting request behaviour. ASP.NET Web API controllers are treated as warning-level review items because they may contain HTTP API routing, model binding, filters, authentication, serialization, or `System.Web` hosting assumptions. Web API actions and route attributes are treated as informational endpoint review items because they identify HTTP API behaviour and URL patterns that may need mapping to ASP.NET Core controllers, minimal APIs, or endpoint routing. Web API action attributes are treated as warning-level review items because HTTP verb, authorization, anonymous access, and accept verbs attributes can materially affect migrated API behaviour. `WebApiConfig.cs`, `MapHttpRoute(...)`, and `GlobalConfiguration.Configure(...)` are treated as informational startup and routing review items because they may define conventional API routes, attribute routing, formatters, filters, dependency resolution, or other Web API configuration that needs explicit ASP.NET Core equivalents. HTTP module and handler registrations from `web.config` are treated as warning-level request pipeline review items because they may affect request lifecycle behaviour and usually need explicit mapping to ASP.NET Core middleware, endpoints, or controllers.

Current legacy ASP.NET artifact discovery is intentionally static and lightweight. It combines file-based discovery with selected source-level ASP.NET MVC and Web API signals and does not require the target solution to build or run.

---

## WCF Endpoint Discovery

LegacyLens.NET can detect WCF endpoint configuration from `app.config` and `web.config` files, including endpoint-level details and selected binding configuration details.

The current WCF scanner looks for `<system.serviceModel>` configuration, extracts endpoint details from configured services, and attempts to resolve selected details from named binding configurations, including security settings, credential settings, timeout values, message size limits, buffer limits, transfer mode, and reader quotas.

Example WCF configuration:

```xml
<configuration>
  <system.serviceModel>
    <bindings>
      <basicHttpBinding>
        <binding
          name="CustomerBinding"
          openTimeout="00:01:00"
          closeTimeout="00:01:00"
          sendTimeout="00:02:00"
          receiveTimeout="00:10:00"
          maxReceivedMessageSize="1048576"
          maxBufferSize="65536"
          maxBufferPoolSize="524288"
          transferMode="Streamed">
          <security mode="Transport">
            <transport clientCredentialType="Windows" />
            <message clientCredentialType="UserName" />
          </security>
          <readerQuotas
            maxDepth="32"
            maxStringContentLength="8192"
            maxArrayLength="16384"
            maxBytesPerRead="4096"
            maxNameTableCharCount="16384" />
        </binding>
      </basicHttpBinding>
    </bindings>

    <services>
      <service name="SampleLegacyApp.Services.CustomerService">
        <endpoint
          address=""
          binding="basicHttpBinding"
          bindingConfiguration="CustomerBinding"
          contract="SampleLegacyApp.Contracts.ICustomerService" />

        <endpoint
          address="mex"
          binding="mexHttpBinding"
          contract="IMetadataExchange" />
      </service>
    </services>
  </system.serviceModel>
</configuration>
```

Example report output:

```markdown
## WCF Endpoints

| Service | Address | Binding | Binding Configuration | Security Mode | Transport Credential | Message Credential | Metadata Exchange | Contract | Config File |
|---|---|---|---|---|---|---|---|---|---|
| SampleLegacyApp.Services.CustomerService |  | basicHttpBinding | CustomerBinding | Transport | Windows | UserName | False | SampleLegacyApp.Contracts.ICustomerService | `...\SampleLegacyApp.Web\Web.config` |
| SampleLegacyApp.Services.CustomerService | mex | mexHttpBinding |  |  |  |  | True | IMetadataExchange | `...\SampleLegacyApp.Web\Web.config` |
```

Additional WCF binding detail report output:

```markdown
## WCF Binding Details

| Service | Binding | Binding Configuration | Open Timeout | Close Timeout | Send Timeout | Receive Timeout | Max Received Message Size | Max Buffer Size | Max Buffer Pool Size | Transfer Mode |
|---|---|---|---|---|---|---|---:|---:|---:|---|
| SampleLegacyApp.Services.CustomerService | basicHttpBinding | CustomerBinding | 00:01:00 | 00:01:00 | 00:02:00 | 00:10:00 | 1048576 | 65536 | 524288 | Streamed |

## WCF Reader Quotas

| Service | Binding | Binding Configuration | Max Depth | Max String Content Length | Max Array Length | Max Bytes Per Read | Max Name Table Char Count |
|---|---|---|---:|---:|---:|---:|---:|
| SampleLegacyApp.Services.CustomerService | basicHttpBinding | CustomerBinding | 32 | 8192 | 16384 | 4096 | 16384 |
```

This helps identify legacy service boundaries and integration points without needing to build or run the target application.

Current WCF endpoint discovery is configuration-based. It does not require the target application to build or run.

---

## WCF Behaviour Discovery

LegacyLens.NET can detect selected WCF service behaviour and endpoint behaviour configuration from `app.config` and `web.config` files.

This is useful because WCF behaviours often contain runtime settings that materially affect migration planning, such as metadata publishing, debug exception detail, throttling, and REST-style `webHttp` endpoint behaviour.

Current WCF behaviour discovery supports:

- service behaviours under `<serviceBehaviors>`
- endpoint behaviours under `<endpointBehaviors>`
- service metadata settings from `<serviceMetadata>`
- `httpGetEnabled`
- `httpsGetEnabled`
- service debug settings from `<serviceDebug>`
- `includeExceptionDetailInFaults`
- service throttling settings from `<serviceThrottling>`
- `maxConcurrentCalls`
- `maxConcurrentSessions`
- `maxConcurrentInstances`
- endpoint `webHttp` behaviour indicators

Example WCF behaviour configuration:

```xml
<configuration>
  <system.serviceModel>
    <services>
      <service
        name="SampleLegacyApp.Services.CustomerService"
        behaviorConfiguration="CustomerServiceBehaviour">
        <endpoint
          address=""
          binding="basicHttpBinding"
          bindingConfiguration="CustomerBinding"
          behaviorConfiguration="JsonEndpointBehaviour"
          contract="SampleLegacyApp.Contracts.ICustomerService" />
      </service>
    </services>

    <behaviors>
      <serviceBehaviors>
        <behavior name="CustomerServiceBehaviour">
          <serviceMetadata httpGetEnabled="true" httpsGetEnabled="false" />
          <serviceDebug includeExceptionDetailInFaults="true" />
          <serviceThrottling
            maxConcurrentCalls="100"
            maxConcurrentSessions="50"
            maxConcurrentInstances="25" />
        </behavior>
      </serviceBehaviors>

      <endpointBehaviors>
        <behavior name="JsonEndpointBehaviour">
          <webHttp />
        </behavior>
      </endpointBehaviors>
    </behaviors>
  </system.serviceModel>
</configuration>
```

Example report output:

```markdown
## WCF Behaviours

| Kind | Name | Service Metadata | HTTP Metadata | HTTPS Metadata | Service Debug | Exception Detail In Faults | Service Throttling | Max Concurrent Calls | Max Concurrent Sessions | Max Concurrent Instances | Web HTTP | Config File |
|---|---|---|---|---|---|---|---|---:|---:|---:|---|---|
| ServiceBehaviour | CustomerServiceBehaviour | True | true | false | True | true | True | 100 | 50 | 25 | False | `...\SampleLegacyApp.Web\Web.config` |
| EndpointBehaviour | JsonEndpointBehaviour | False |  |  | False |  | False |  |  |  | True | `...\SampleLegacyApp.Web\Web.config` |
```

These behaviour details are also used as modernisation hint inputs. Service behaviours and endpoint behaviours are reported as informational WCF behaviour findings. Metadata publishing settings are reported as WCF metadata findings. `includeExceptionDetailInFaults="true"` is reported as a warning-level WCF debug finding. Service throttling is reported as a warning-level WCF throttling finding. `webHttp` endpoint behaviour is reported as a warning-level WCF REST finding.

Current WCF behaviour discovery is configuration-based. It does not require the target application to build or run.

> Note: the codebase uses the British spelling `Behaviour` for model and report names, while WCF XML uses the standard WCF element names `<behaviors>`, `<serviceBehaviors>`, `<endpointBehaviors>`, and `<behavior>`.

---

## WCF Service Contract Discovery

LegacyLens.NET can also detect WCF service contracts from C# source files.

The current WCF service contract scanner looks for interfaces marked with `[ServiceContract]`, `[ServiceContract(...)]`, or `[ServiceContractAttribute]`, and operations marked with `[OperationContract]`, `[OperationContract(...)]`, or `[OperationContractAttribute]`.

Operation discovery is scoped to the containing service contract interface. This means multiple service contracts can exist in the same source file without operations being incorrectly shared between contracts.

Example WCF service contract:

```csharp
using System.ServiceModel;

namespace SampleLegacyApp.Contracts;

[ServiceContract]
public interface ICustomerService
{
    [OperationContract]
    CustomerDto GetCustomer(int id);
}
```

Example report output:

```markdown
## WCF Service Contracts

| Contract | Operations | Source File |
|---|---|---|
| ICustomerService | GetCustomer | `...\SampleLegacyApp.Contracts\CustomerContracts.cs` |
```

Multiple service contracts can be declared in the same source file:

```csharp
using System.ServiceModel;

namespace SampleLegacyApp.Contracts;

[ServiceContract]
public interface ICustomerService
{
    [OperationContract]
    CustomerDto GetCustomer(int id);
}

[ServiceContract]
public interface IOrderService
{
    [OperationContract]
    OrderDto GetOrder(int id);
}
```

These are reported as separate contracts with their own operation lists:

```markdown
## WCF Service Contracts

| Contract | Operations | Source File |
|---|---|---|
| ICustomerService | GetCustomer | `...\Contracts.cs` |
| IOrderService | GetOrder | `...\Contracts.cs` |
```

This helps identify service boundaries defined in code, even when the target solution cannot be built or run.

Current service contract discovery is intentionally lightweight and static. It is based on source scanning rather than compilation, so it does not require the target solution to build.

---

## Upgrade Readiness Analysis

The `upgrade-readiness` capability is an MVP-scope analysis that produces a separate Markdown artifact named `upgrade-readiness-report.md`.

It should consume existing static discovery results rather than duplicating scanners where possible. Useful inputs include:

- solution and project structure
- project names and project file paths
- current project target frameworks
- project references and coupling signals
- package references, versions, source format, source path, and package target framework where available
- assembly references, including `System.Web` and `System.ServiceModel` indicators
- WCF endpoint, binding, behaviour, and service contract evidence
- legacy ASP.NET artifacts such as WebForms, ASMX, ASHX, `Global.asax`, MVC, and Web API indicators
- configuration file evidence from `app.config` and `web.config`
- configuration counts such as app settings, connection strings, and custom sections
- modernisation hints already produced by the tool

The report should answer which projects look like lower-risk candidates, which require moderate review, which should be reviewed first, and which cannot be classified confidently from static evidence.

Readiness categories should be limited to:

- `Lower risk candidate`
- `Moderate review required`
- `Higher risk / review first`
- `Unknown`

Initial static concern rules should include:

| Evidence | Possible concern |
|---|---|
| `.NET Framework` target framework | Requires review before moving to modern .NET |
| `packages.config` | Older NuGet package management style; PackageReference migration may be needed |
| `System.Web` assembly reference | ASP.NET Core does not use the System.Web pipeline |
| `Global.asax` | Startup/application lifecycle logic may need migration |
| `.aspx`, `.ascx`, `.master` | WebForms artifacts may require rewrite or replacement |
| `.asmx` | Legacy ASMX service surface may require replacement |
| `.ashx` | Custom HTTP handler may require ASP.NET Core middleware or endpoint replacement |
| `System.ServiceModel` or WCF configuration | WCF migration decision required |
| `EntityFramework` package | EF6 migration or isolation decision required |
| `.edmx`, `DbContext`, or `ObjectContext` where detected | Data access migration requires review |
| direct assembly references with `HintPath` where detected | Local or vendor DLL compatibility may need review |
| connection strings | Database/runtime dependency requires migration planning |
| custom config sections | Runtime configuration may need replacement or migration |

The analysis must use cautious wording such as `Possible concern`, `Requires review`, `Evidence found`, `May need migration work`, and `Likely upgrade consideration`. It should avoid absolute wording such as `Unsupported`, `Cannot be upgraded`, `Safe to migrate`, `Guaranteed compatible`, `Unused`, or `Must rewrite`.

### Upgrade Readiness Report

The generated report should include:

- Summary
- Target
- Current Project Targets
- Upgrade Readiness Overview
- Project Upgrade Candidates
- Possible Upgrade Concerns
- Package Upgrade Considerations
- Assembly Reference Considerations
- Configuration and Runtime Considerations
- Suggested Review Order
- Notes and Limitations

## Upgrade Blockers Analysis

The `upgrade-blockers` capability is an MVP-scope analysis that produces a separate Markdown artifact named `upgrade-blockers.md`.

It should consume existing static discovery results rather than duplicating scanners where possible. Useful inputs include:

- solution and project structure
- project names and project file paths
- current project target frameworks
- package references, versions, source format, source path, and package target framework where available
- assembly references, including `System.Web` and `System.ServiceModel` indicators
- direct assembly, local DLL, `HintPath`, and COM reference evidence where available
- WCF endpoint, binding, behaviour, configuration, and service contract evidence
- legacy ASP.NET artifacts such as WebForms, ASMX, ASHX, `Global.asax`, HTTP modules, HTTP handlers, MVC, and Web API indicators
- EF6, EDMX, `ObjectContext`, and `DbContext` evidence where discoverable
- configuration file evidence from `app.config` and `web.config`
- configuration counts such as app settings, connection strings, and custom sections
- existing modernisation hints and package compatibility/static package review information

Where `upgrade-readiness` answers how ready the solution looks for upgrade, `upgrade-blockers` should answer what visible blockers and decisions could stop or complicate the upgrade. It should be focused, direct, and decision-oriented.

Blocker categories should be limited to a small set:

- `Legacy ASP.NET / System.Web`
- `WCF / ServiceModel`
- `EF6 / EDMX / Data Access`
- `Package Management`
- `Direct Assembly References`
- `Configuration / Runtime Coupling`
- `Windows-only / Platform-specific APIs`
- `Custom Build / MSBuild Behaviour`
- `Unknown / Requires Manual Review`

Impact labels should be limited to:

- `High`
- `Medium`
- `Low`
- `Unknown`

Initial static blocker rules should include:

| Evidence | Category | Impact | Possible blocker / decision |
|---|---|---|---|
| `System.Web` reference | Legacy ASP.NET / System.Web | High | ASP.NET Core does not use the `System.Web` pipeline |
| `Global.asax` | Legacy ASP.NET / System.Web | High | Application startup/lifecycle logic requires migration review |
| `.aspx`, `.ascx`, `.master` | Legacy ASP.NET / System.Web | High | WebForms artifacts may require rewrite or replacement |
| `.asmx` | Legacy ASP.NET / System.Web | High | Legacy ASMX service surface may require replacement |
| `.ashx` | Legacy ASP.NET / System.Web | High | HTTP handler may require middleware/endpoint replacement |
| HTTP module or handler registration | Legacy ASP.NET / System.Web | High | Custom request pipeline behaviour requires migration review |
| `System.ServiceModel` reference | WCF / ServiceModel | High | WCF migration decision required |
| `system.serviceModel` config section | WCF / ServiceModel | High | WCF endpoint/binding/security config requires review |
| `EntityFramework` package | EF6 / EDMX / Data Access | Medium/High | EF6 migration or isolation decision required |
| `.edmx` file | EF6 / EDMX / Data Access | High | EDMX/ObjectContext migration is likely non-mechanical |
| `ObjectContext` | EF6 / EDMX / Data Access | High | ObjectContext-based data access needs migration review |
| `DbContext` | EF6 / EDMX / Data Access | Medium | Data access needs review before EF Core migration |
| `packages.config` | Package Management | Medium | PackageReference migration may be needed |
| Direct assembly reference with `HintPath` | Direct Assembly References | Medium/High | Local/vendor DLL compatibility requires review |
| COM reference | Direct Assembly References | High | COM dependency may block cross-platform migration |
| `App.config` / `Web.config` | Configuration / Runtime Coupling | Medium | Runtime configuration requires migration planning |
| connection strings | Configuration / Runtime Coupling | Medium | Database/runtime dependency requires review |
| binding redirects | Configuration / Runtime Coupling | Medium | Assembly binding behaviour changes under modern .NET |
| custom config sections | Configuration / Runtime Coupling | Medium | Custom configuration may require replacement |
| pre-build/post-build event | Custom Build / MSBuild Behaviour | Medium | Build behaviour may need migration into SDK-style projects |
| custom `.targets` / `.props` | Custom Build / MSBuild Behaviour | Medium | Custom MSBuild logic requires review |

The analysis must use cautious wording such as `Possible blocker`, `Potential blocker`, `Requires review`, `Migration decision required`, `Evidence found`, `May complicate upgrade`, and `May require replacement or redesign`. It should avoid absolute wording such as `Impossible to upgrade`, `Unsupported`, `Must rewrite`, `Safe`, `Guaranteed compatible`, `Definitely unused`, or `Cannot be upgraded`.

### Upgrade Blockers Report

The generated report should include:

- Summary
- Target
- Blocker Overview
- Upgrade Blockers and Decisions
- Blocker Details
- category-specific evidence tables
- decisions required for each blocker category
- Suggested Review Order
- Notes and Limitations

---


## External Dependencies Analysis

The `external-dependencies` capability is an MVP-scope analysis that produces a separate Markdown artifact named `external-dependencies.md`.

It should consume existing static discovery results rather than duplicating scanners where possible. Useful inputs include:

- configuration files such as `App.config`, `Web.config`, config transforms, `appsettings.json`, and `.settings` files where discoverable
- `appSettings` keys and values where the implementation captures them
- `connectionStrings` entries and provider names where available
- WCF endpoint, binding, behaviour, and service contract evidence
- project package references, versions, source format, and source path
- assembly references and direct `HintPath` evidence where available
- source code string literals where feasible and low-risk to scan
- `NuGet.config` package sources where discoverable
- existing modernisation hints where they provide useful supporting evidence

The purpose is to identify possible systems or resources outside the repository that the application may depend on at runtime or build time. The report should answer what external systems appear to be referenced, where the evidence was found, what category the dependency belongs to, and what should be confirmed by the development team before migration, testing, deployment, onboarding, or local development.

Dependency categories should be limited to a small set:

- `Database`
- `HTTP / API`
- `WCF / Service Endpoint`
- `Messaging / Queue`
- `File System / File Share`
- `Email / SMTP`
- `Cache / Distributed State`
- `Authentication / Identity Provider`
- `Cloud Service`
- `Private Package Feed`
- `External Assembly / Vendor DLL`
- `Unknown / Requires Review`

Initial static evidence rules should include:

| Evidence | Category | Possible finding |
|---|---|---|
| `connectionStrings` section | Database | Database dependency configured |
| `System.Data.SqlClient`, `Microsoft.Data.SqlClient`, `Npgsql`, `MySql.Data`, `MySqlConnector`, or `Oracle.ManagedDataAccess` package/reference | Database | Database technology dependency may exist |
| URL-looking config value such as `http://` or `https://` | HTTP / API | External or internal HTTP service dependency may exist |
| App setting key ending with `Url`, `Uri`, `Endpoint`, `BaseAddress`, `BaseUrl`, or `ApiUrl` | HTTP / API | Service endpoint configuration may exist |
| `system.serviceModel/client/endpoint` or configured WCF endpoint | WCF / Service Endpoint | WCF client/service endpoint configured |
| `System.ServiceModel` reference | WCF / Service Endpoint | WCF usage may indicate service dependency |
| `RabbitMQ.Client`, `MassTransit`, `NServiceBus`, `Microsoft.Azure.ServiceBus`, or `Azure.Messaging.ServiceBus` package/reference | Messaging / Queue | Message broker or queue dependency may exist |
| App setting containing `Queue`, `Topic`, or `Subscription` | Messaging / Queue | Messaging dependency may exist |
| UNC path beginning `\\` | File System / File Share | Network file share dependency may exist |
| Windows absolute path such as `C:\...` | File System / File Share | Local machine path dependency may exist |
| App setting containing `Path`, `Folder`, `Directory`, or `Share` | File System / File Share | File system dependency may exist |
| SMTP config, `SmtpClient`, `SendGrid`, or `MailKit` evidence | Email / SMTP | Email service dependency may exist |
| `StackExchange.Redis` package/reference or app setting containing `Redis`, `Cache`, or `DistributedCache` | Cache / Distributed State | Cache dependency may exist |
| `ida:ClientId`, `ida:Tenant`, `Authority`, `Issuer`, `Audience`, OpenID Connect, Microsoft Identity, or Azure Identity evidence | Authentication / Identity Provider | Identity provider dependency may exist |
| `WindowsAzure.Storage`, `Azure.Storage.Blobs`, `Microsoft.ApplicationInsights`, `AWSSDK.*`, or `Google.Cloud.*` package/reference | Cloud Service | Cloud service dependency may exist |
| `NuGet.config` non-nuget.org package source | Private Package Feed | Private package feed dependency may exist |
| Direct assembly reference with `HintPath` | External Assembly / Vendor DLL | Vendor/local DLL dependency may exist |

Each finding should include evidence such as category, name or identifier, source type, source file, project name where applicable, evidence summary, masked value where necessary, confidence level if useful, notes, and whether confirmation is required.

The analysis must be cautious and security-conscious. It should use wording such as `Possible external dependency`, `Evidence found`, `Requires confirmation`, `Configured dependency`, `Code reference`, `May indicate dependency`, and `Potential runtime dependency`. It should avoid absolute wording such as `Definitely used`, `Production dependency`, `Safe to remove`, `Unused`, `Verified`, `Reachable`, `Credential is valid`, or `Complete dependency map`.

Sensitive values should not be printed in full. Passwords, API keys, tokens, connection string passwords, SAS tokens, access keys, client secrets, certificate private keys, private feed credentials, and embedded credentials should be masked or redacted.

### External Dependencies Report

The generated report should include:

- Summary
- Analysis Scope
- Dependency Overview
- Dependencies
- Database Dependencies
- HTTP / Service Dependencies
- WCF Dependencies
- Messaging Dependencies
- File System Dependencies
- Email Dependencies
- Cache / Distributed State Dependencies
- Build-Time / Package Feed Dependencies
- Suggested Questions to Ask the Team
- Notes and Limitations

---


## Configuration Inventory Analysis

The `configuration-inventory` capability should analyse visible repository configuration evidence and produce `configuration-inventory.md` without building or running the solution.

Current MVP discovery expectations include:

- finding common configuration files such as `App.config`, `Web.config`, `*.config`, `appsettings.json`, `appsettings.*.json`, `.settings` files, and useful build/package configuration files such as `NuGet.config` where relevant
- associating configuration findings with the nearest discovered project where possible, so settings from a project-owned file such as `Web.config` are reported against that project rather than `Unknown`
- detecting `appSettings` entries from XML configuration files and preserving key names with safe values
- detecting `connectionStrings` entries from XML configuration files and preserving name, provider name where available, source file, and safe value
- detecting custom configuration sections from `configSections`
- detecting environment-specific transform files such as `Web.Release.config`
- detecting WCF `system.serviceModel` sections
- detecting ASP.NET/IIS `system.web` and `system.webServer` sections
- detecting binding redirects
- detecting authentication and authorization sections
- detecting logging and diagnostics sections such as `system.diagnostics`, `log4net`, `nlog`, or `serilog`
- detecting Entity Framework configuration sections
- detecting SMTP and mail settings
- detecting configuration API usage in source files, such as `ConfigurationManager.AppSettings`, `ConfigurationManager.ConnectionStrings`, `IConfiguration`, and `GetSection`
- flattening JSON configuration files into configuration-path keys where feasible, for example `ConnectionStrings:RabbitMQ`, `RabbitMQ:HostName`, and `RabbitMQ:QueueName`
- reporting JSON scalar values with safe masking rather than only reporting that an `appsettings*.json` file exists, where feasible
- treating structural findings, such as file presence or section presence, as findings with no scalar value rather than unknown values
- applying value-aware masking so useful non-secret parts are preserved and sensitive parts are redacted, for example connection-string passwords, URI credentials, API keys, tokens, and client secrets

The generated `configuration-inventory.md` report should include summary counts, analysis scope, configuration overview, grouped configuration values by source file, suggested files to review first, migration considerations, suggested questions to ask the team, and notes/limitations.

The detailed findings should be grouped first by project and source file, then by category within each file. Per-file tables should use compact columns such as `Name`, `Value`, `Evidence`, and `Requires Review`. The report should use `Value` to mean "the discovered value with sensitive parts masked where needed". For structural findings with no scalar value, such as `Web.config`, `system.serviceModel`, `bindingRedirect`, or `NuGet.config`, the report should show `N/A` rather than `Unknown`.

Out of scope for MVP:

- running the application
- applying config transforms
- validating config syntax beyond safe parsing where implemented
- validating credentials or connection strings
- connecting to configured services
- proving production runtime behaviour
- proving that a setting is used or unused
- fully evaluating runtime configuration inheritance
- resolving all deployment-time substitutions
- guaranteeing completeness
- exposing full secrets or sensitive values

## Data Access Analysis

The `data-access` capability is an MVP-scope static analysis that should produce `data-access-inventory.md`. It should help a developer understand how the codebase appears to access databases and persistence infrastructure.

The analysis should use repository evidence where available, including:

- project names, project file paths, and target frameworks
- package references, package versions, package source formats, and package target frameworks where available
- assembly references
- `app.config`, `web.config`, and `appsettings.json` where available
- connection strings and provider names, with sensitive values masked or redacted
- `.edmx` files and EF-related `.tt` T4 templates
- `.dbml` LINQ to SQL files
- source files containing data access indicators
- `DbContext`, `ObjectContext`, and EF Core `DbContext` candidates
- ADO.NET usage such as `SqlConnection`, `SqlCommand`, `DbConnection`, `DbCommand`, and `CommandType.StoredProcedure`
- Dapper usage such as Dapper package references or `Query` / `Execute` extension method indicators where feasible
- NHibernate usage such as package references, `ISession`, `SessionFactory`, or mapping files where feasible
- raw SQL strings where feasible
- possible stored procedure names where feasible
- repository and unit-of-work class names
- migration folders where feasible
- existing package compatibility or modernisation hints where relevant

Suggested data access categories:

- `Connection String`
- `Entity Framework 6`
- `Entity Framework Core`
- `EDMX / ObjectContext`
- `ADO.NET`
- `Dapper`
- `NHibernate`
- `LINQ to SQL`
- `Raw SQL`
- `Stored Procedure`
- `Repository Pattern`
- `Unit of Work Pattern`
- `Database Provider`
- `Migration Artifact`
- `Unknown / Requires Review`

Initial static rules should be simple and evidence-backed:

| Evidence | Category | Finding |
|---|---|---|
| `connectionStrings` section | Connection String | Database connection string configured |
| `providerName="System.Data.SqlClient"` or `providerName="Microsoft.Data.SqlClient"` | Database Provider | SQL Server provider detected |
| `providerName` containing `Npgsql` | Database Provider | PostgreSQL provider detected |
| `providerName` containing `MySql` | Database Provider | MySQL provider detected |
| `providerName` containing `Oracle` | Database Provider | Oracle provider detected |
| `EntityFramework` package | Entity Framework 6 | EF6 package detected |
| `Microsoft.EntityFrameworkCore` package | Entity Framework Core | EF Core package detected |
| `.edmx` file | EDMX / ObjectContext | EDMX model detected |
| `.tt` file near EDMX | EDMX / ObjectContext | EF T4 template detected |
| `.dbml` file | LINQ to SQL | LINQ to SQL model detected |
| `SqlConnection`, `SqlCommand`, `DbConnection`, or `DbCommand` | ADO.NET | ADO.NET usage detected |
| `CommandType.StoredProcedure` or `EXEC` / `EXECUTE` strings | Stored Procedure | Possible stored procedure usage detected |
| `SELECT`, `INSERT`, `UPDATE`, `DELETE`, `MERGE`, or `EXEC` strings | Raw SQL | Possible raw SQL detected |
| `Dapper` package or common Dapper calls | Dapper | Dapper usage detected |
| `NHibernate` package or `ISession` / `SessionFactory` indicators | NHibernate | NHibernate usage detected |
| Class names ending in `Repository` | Repository Pattern | Repository candidate detected |
| Class names containing `UnitOfWork` or `IUnitOfWork` | Unit of Work Pattern | Unit-of-work candidate detected |
| Folder names such as `Migrations` | Migration Artifact | Migration artifact detected |

The data-access report should use cautious wording such as `Evidence found`, `Possible data access dependency`, `Requires review`, `May indicate database usage`, `Possible stored procedure usage`, `Migration consideration`, and `Should be verified by the development team`. It should avoid absolute wording such as `Definitely used in production`, `Safe to remove`, `Unused`, `Guaranteed compatible`, `Automatically migratable`, or `Must rewrite`.

### Data Access Report

The report should include summary, analysis scope, data access overview, projects with data access indicators, connection strings, ORM and data access technologies, EF/EDMX details, DbContext/ObjectContext candidates, repository and unit-of-work candidates, raw SQL and stored procedure indicators, database provider indicators, suggested files to review first, migration considerations, suggested questions to ask the team, and notes/limitations.

## Modernisation Hint Analysis

LegacyLens.NET can produce basic modernisation hints from the information it discovers.

The current modernisation hint analysis is intentionally lightweight. It does not attempt to fully assess migration effort, but it highlights useful review areas for developers investigating a legacy or unfamiliar .NET codebase.

Current hint areas include:

- target framework review
- project dependency review
- package review
- legacy ASP.NET / `System.Web` review
- legacy ASP.NET artifact review
- legacy ASP.NET request pipeline review
- ASP.NET HTTP module registration review
- ASP.NET HTTP handler registration review
- ASP.NET MVC area registration review
- ASP.NET MVC startup registration review
- ASP.NET MVC bundle configuration review
- ASP.NET MVC filter configuration review
- ASP.NET Web API controller review
- ASP.NET Web API action review
- ASP.NET Web API route attribute review
- ASP.NET Web API action attribute review
- ASP.NET Web API configuration review
- ASP.NET Web API startup registration review
- ASP.NET Web API route registration review
- WCF endpoint review
- WCF binding review
- WCF endpoint configuration review
- WCF security review
- WCF timeout review
- WCF message size and buffer limit review
- WCF transfer mode review
- WCF reader quota review
- WCF metadata exchange review
- WCF service contract review
- WCF behaviour review
- WCF metadata publishing review
- WCF debug setting review
- WCF throttling review
- WCF REST-style endpoint behaviour review
- configuration-heavy application review
- configuration review

Current severity levels are:

| Severity | Meaning |
|---|---|
| `Info` | Useful information to review during discovery |
| `Warning` | Something that may need extra attention |
| `Risk` | Something likely to affect modernisation or migration planning |

Modernisation hints also include evidence metadata where a clear source can be identified.

Modernisation hints are de-duplicated after evidence metadata has been attached. This means exact duplicate findings are removed from the final hint list using the final visible hint details, including severity, area, finding, reason, evidence kind, evidence name, evidence path, and confidence.

WCF endpoint-level binding hints include available contract and binding configuration details. This helps distinguish multiple endpoints on the same service that use the same binding but represent different contracts or named binding configurations.

Current hint evidence fields are:

| Field | Meaning |
|---|---|
| `EvidenceKind` | The type of discovered item that supports the hint, such as `Project`, `PackageReference`, `AssemblyReference`, `WcfEndpoint`, `WcfServiceContract`, `WcfBehaviour`, `LegacyAspNetArtifact`, `ConfigurationFile`, or `AnalysisSummary`. |
| `EvidenceName` | The name of the supporting item, such as a package name, assembly name, project name, service name, behaviour name, contract name, artifact name, or configuration file name. |
| `EvidencePath` | The source file, project file, or configuration file path where the supporting evidence was found, where available. |
| `Confidence` | A lightweight confidence value for the evidence mapping. Current values are `Low`, `Medium`, and `High`. |

The generated Markdown report renders this metadata as `Evidence`, `Confidence`, and `Source` columns in the `Modernisation Hints` table.

Example:

```markdown
## Modernisation Hints

| Severity | Area | Finding | Evidence | Confidence | Source | Reason |
|---|---|---|---|---|---|---|
| Risk | Target Framework | SampleLegacyApp.Web targets net48 | Project: SampleLegacyApp.Web | High | `...\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj` | .NET Framework projects usually need extra assessment before migration to modern .NET. |
| Warning | WCF Transfer Mode | SampleLegacyApp.Services.CustomerService uses WCF transfer mode Streamed | WcfEndpoint: SampleLegacyApp.Services.CustomerService | High | `...\SampleLegacyApp.Web\Web.config` | Streaming WCF transfer modes may affect endpoint redesign, request buffering, file upload/download behaviour, hosting limits, and client compatibility. |
| Warning | Legacy ASP.NET MVC Attributes | HomeController.Index [HttpGet] uses an ASP.NET MVC action attribute | LegacyAspNetArtifact: HomeController.Index [HttpGet] | High | `...\SampleLegacyApp.Web\Controllers\HomeController.cs` | MVC action attributes such as HTTP verb, authorization, anonymous access, anti-forgery, and output caching attributes may affect behaviour during ASP.NET Core migration. |
| Warning | Legacy ASP.NET Request Pipeline | LegacyAuthModule registers an ASP.NET HTTP module | LegacyAspNetArtifact: LegacyAuthModule | High | `...\SampleLegacyApp.Web\Web.config` | HTTP modules can affect authentication, authorization, logging, headers, errors, or request lifecycle behaviour and may need mapping to ASP.NET Core middleware. |
```

Evidence metadata is intended to make each hint more explainable. The review summary identifies where to look first, while the detailed hint table shows the supporting evidence that contributed to those review areas.

Current WCF binding hints include:

| Binding | Severity | Meaning |
|---|---|---|
| Missing binding | `Warning` | The endpoint cannot be fully assessed because binding information is missing |
| `basicHttpBinding` | `Warning` | Commonly indicates SOAP interoperability that may need replacement or compatibility planning |
| `wsHttpBinding` | `Warning` | May indicate SOAP and WS-* features that need modernisation assessment |
| `netTcpBinding` | `Risk` | WCF-specific communication that usually needs careful migration or replacement planning |
| `netMsmqBinding` | `Risk` | Queue-based WCF integration that needs separate migration planning |

For example, if two `basicHttpBinding` endpoints exist on the same service but use different contracts or binding configurations, the generated hints are intentionally distinct:

```text
basicHttpBinding endpoint discovered for SampleLegacyApp.Services.CustomerService contract SampleLegacyApp.Contracts.ICustomerContract
basicHttpBinding endpoint discovered for SampleLegacyApp.Services.CustomerService contract SampleLegacyApp.Contracts.ICustomerService using binding configuration CustomerBinding
```

This avoids duplicate-looking findings while preserving the fact that both endpoints may need review.

Current WCF endpoint detail hints include:

| Indicator | Severity | Meaning |
|---|---|---|
| Named binding configuration | `Info` | Named WCF binding configurations may contain security, timeout, size, protocol, or credential settings that need migration review |
| Security mode other than `None` | `Warning` | WCF security settings need explicit review when replacing WCF endpoints with modern HTTP, JSON, gRPC, or other service endpoints |
| Transport credential type other than `None` | `Warning` | Transport credential settings may affect authentication and hosting choices during service migration |
| Metadata exchange endpoint | `Info` | Metadata exchange endpoints are useful discovery signals when identifying SOAP contracts and generated client dependencies |


Current WCF operational detail hints include:

| Indicator | Severity | Meaning |
|---|---|---|
| Explicit timeout settings | `Info` | Configured WCF timeout values should be reviewed because modern HTTP, JSON, gRPC, hosting, gateway, and client timeout behaviour may differ |
| Explicit message size or buffer limits | `Info` | Configured WCF message size and buffer limits should be reviewed because equivalent request, response, and hosting limits may need to be set explicitly |
| Explicit non-streaming transfer mode | `Info` | Explicit transfer mode settings should be reviewed because modern hosting and client behaviour may differ |
| Streaming transfer mode | `Warning` | Streaming transfer modes may affect endpoint redesign, request buffering, file upload/download behaviour, hosting limits, and client compatibility |
| Reader quota settings | `Warning` | Reader quotas may affect XML payload compatibility, maximum object graph depth, string sizes, array sizes, and generated SOAP client behaviour during migration |


Current WCF behaviour hints include:

| Indicator | Severity | Meaning |
|---|---|---|
| WCF service behaviour | `Info` | Service behaviours can contain metadata, debug, throttling, credential, authorization, and runtime settings that need migration review |
| WCF endpoint behaviour | `Info` | Endpoint behaviours can affect request handling, serialization, dispatch, message inspection, and REST-style endpoint behaviour |
| Service metadata configured | `Info` | Service metadata settings are useful discovery signals when identifying SOAP contracts, generated clients, and compatibility requirements |
| HTTP or HTTPS metadata publishing enabled | `Info` | Metadata publishing may indicate externally discoverable SOAP metadata that clients depend on |
| `includeExceptionDetailInFaults="true"` | `Warning` | Exception detail in faults can expose implementation details and should be reviewed before moving to modern hosting or public endpoints |
| Service throttling configured | `Warning` | WCF throttling settings may need explicit equivalents in modern hosting, gateway, queue, or service runtime configuration |
| `webHttp` endpoint behaviour | `Warning` | Usually indicates REST-style WCF endpoints that need separate review when migrating to ASP.NET Core APIs |


Current legacy ASP.NET hints include:

| Indicator | Severity | Meaning |
|---|---|---|
| `System.Web` assembly reference | `Risk` | Usually indicates classic ASP.NET, WebForms, MVC 5, ASMX, or ASP.NET-hosted legacy functionality that does not directly migrate to modern ASP.NET Core |
| `System.Web.*` assembly reference | `Warning` | Indicates legacy ASP.NET-related functionality that may need separate migration assessment |
| `.aspx` WebForms page | `Risk` | WebForms UI usually needs redesign or replacement planning when moving to modern ASP.NET |
| `.asmx` ASMX web service | `Risk` | ASMX web services are legacy SOAP-style ASP.NET endpoints that usually need replacement or compatibility planning |
| `.ascx` WebForms user control | `Warning` | User controls may contain reusable UI and page lifecycle behaviour that needs review |
| `.master` WebForms master page | `Warning` | Master pages usually indicate shared WebForms layout structure that may need redesign |
| `.ashx` HTTP handler | `Warning` | HTTP handlers may contain custom request processing that needs mapping to middleware, endpoints, or controllers |
| `Global.asax` application file | `Info` | May contain startup, routing, error handling, or application lifecycle code that should be reviewed |
| ASP.NET MVC controller | `Warning` | MVC controllers may contain routing, action filters, model binding, authentication, or `System.Web`-specific behaviour that needs review when moving to ASP.NET Core |
| ASP.NET MVC action | `Info` | MVC actions identify request-handling behaviour that should be reviewed for routing, model binding, result shape, filters, and ASP.NET Core controller migration |
| ASP.NET MVC route attribute | `Info` | Attribute routes should be mapped carefully to ASP.NET Core endpoint routing to preserve URL patterns, defaults, constraints, and client compatibility |
| ASP.NET MVC action attribute | `Warning` | MVC action attributes such as HTTP verb, authorization, anonymous access, anti-forgery, and output caching attributes may affect behaviour during ASP.NET Core migration |
| ASP.NET MVC area registration | `Info` | Area registrations may define area-specific routes and feature boundaries that should be reviewed when migrating to ASP.NET Core endpoint routing |
| `RouteConfig.cs` route configuration | `Info` | Route configuration may define URL patterns, defaults, constraints, or ignored routes that should be reviewed when migrating to endpoint routing in ASP.NET Core |
| ASP.NET MVC application startup | `Info` | `Application_Start` may contain route, filter, bundle, dependency injection, error handling, or lifecycle registration that needs mapping to ASP.NET Core hosting |
| ASP.NET MVC area registration call | `Info` | `AreaRegistration.RegisterAllAreas()` identifies MVC area routing setup that should be reviewed during endpoint routing migration |
| ASP.NET MVC route registration call | `Info` | `RouteConfig.RegisterRoutes(...)` identifies conventional route setup that should be mapped carefully to ASP.NET Core endpoint routing |
| ASP.NET MVC bundle configuration or registration | `Warning` | ASP.NET MVC bundling and minification usually need replacement with a modern static asset, build, or bundling strategy |
| ASP.NET MVC filter configuration or registration | `Warning` | Global filters can affect authorization, error handling, caching, model binding, or other cross-cutting request behaviour during migration |
| ASP.NET Web API controller | `Warning` | Web API controllers may contain HTTP API behaviour, routing, model binding, filters, authentication, serialization, or `System.Web` hosting assumptions that need review |
| ASP.NET Web API action | `Info` | Web API actions identify HTTP endpoint behaviour that should be reviewed for routing, request and response shape, model binding, filters, and ASP.NET Core API migration |
| ASP.NET Web API route attribute | `Info` | Web API attribute routes should be mapped carefully to ASP.NET Core endpoint routing to preserve URL patterns, defaults, constraints, and client compatibility |
| ASP.NET Web API action attribute | `Warning` | Web API action attributes such as HTTP verb, authorization, anonymous access, and accept verbs attributes may affect endpoint behaviour during migration |
| `WebApiConfig.cs` Web API configuration | `Info` | Web API configuration may define routing, formatters, filters, services, dependency resolution, or other API behaviour that should be mapped during ASP.NET Core migration |
| ASP.NET Web API route registration call | `Info` | `MapHttpRoute(...)` identifies conventional Web API route setup that should be mapped carefully to ASP.NET Core endpoint routing |
| ASP.NET Web API startup registration call | `Info` | `GlobalConfiguration.Configure(...)` and `WebApiConfig.Register(...)` identify Web API startup configuration that may need explicit ASP.NET Core equivalents |

Current configuration hints include:

| Indicator | Severity | Meaning |
|---|---|---|
| Many `appSettings` entries | `Warning` | May indicate environment-specific behaviour or operational settings hidden in configuration |
| One or more `connectionStrings` | `Info` | Identifies external data dependencies that should be reviewed during migration planning |
| One or more custom configuration sections | `Warning` | May indicate framework-specific or application-specific behaviour that needs migration assessment |

Example report output:

```markdown
## Modernisation Hints

| Severity | Area | Finding | Reason |
|---|---|---|---|
| Risk | Target Framework | SampleLegacyApp.Web targets net48 | .NET Framework projects usually need extra assessment before migration to modern .NET. |
| Risk | Packages | SampleLegacyApp.Web references System.ServiceModel.Http | System.ServiceModel packages indicate WCF-related usage, which is important for package compatibility and modernisation planning. |
| Risk | WCF | 1 WCF endpoint(s) discovered | Configured WCF endpoints usually represent service boundaries or integration points that need migration assessment. |
| Info | WCF Configuration | SampleLegacyApp.Services.CustomerService uses binding configuration CustomerBinding | Named WCF binding configurations may contain security, timeout, size, protocol, or credential settings that need migration review. |
| Warning | WCF Security | SampleLegacyApp.Services.CustomerService uses WCF security mode Transport | WCF security settings need explicit review when replacing WCF endpoints with modern HTTP, JSON, gRPC, or other service endpoints. |
| Warning | WCF Security | SampleLegacyApp.Services.CustomerService uses transport credential type Windows | Transport credential settings may affect authentication and hosting choices during service migration. |
| Info | WCF Timeout | SampleLegacyApp.Services.CustomerService has explicit WCF timeout settings | Configured WCF timeout values should be reviewed when replacing endpoints because modern HTTP, JSON, gRPC, hosting, gateway, and client timeout behaviour may differ. |
| Info | WCF Binding Limits | SampleLegacyApp.Services.CustomerService has explicit WCF message size or buffer limits | Configured WCF message size and buffer limits should be reviewed when migrating endpoints because equivalent request, response, and hosting limits may need to be set explicitly. |
| Warning | WCF Transfer Mode | SampleLegacyApp.Services.CustomerService uses WCF transfer mode Streamed | Streaming WCF transfer modes may affect endpoint redesign, request buffering, file upload/download behaviour, hosting limits, and client compatibility. |
| Warning | WCF Reader Quotas | SampleLegacyApp.Services.CustomerService has explicit WCF reader quota settings | Configured WCF reader quotas may affect XML payload compatibility, maximum object graph depth, string sizes, array sizes, and generated SOAP client behaviour during migration. |
| Info | WCF Metadata | SampleLegacyApp.Services.CustomerService exposes a metadata exchange endpoint | Metadata exchange endpoints are useful discovery signals when identifying SOAP contracts and generated client dependencies. |
| Risk | Legacy ASP.NET | SampleLegacyApp.Web references System.Web | System.Web usually indicates classic ASP.NET, WebForms, MVC 5, ASMX, or ASP.NET-hosted legacy functionality that does not directly migrate to modern ASP.NET Core. |
| Risk | Legacy ASP.NET | Default.aspx is a WebForms page | WebForms pages indicate classic ASP.NET UI that does not directly migrate to ASP.NET Core and usually needs redesign or replacement planning. |
| Risk | Legacy ASP.NET | CustomerService.asmx is an ASMX web service | ASMX web services are legacy SOAP-style ASP.NET endpoints that usually need replacement or compatibility planning during modernisation. |
| Warning | Legacy ASP.NET | SampleLegacyApp.Web references System.Web.Mvc | System.Web-related assemblies indicate legacy ASP.NET functionality that may need separate migration assessment. |
| Warning | Legacy ASP.NET | CustomerSummary.ascx is a WebForms user control | WebForms user controls may contain reusable UI and page lifecycle behaviour that needs review during ASP.NET Core migration planning. |
| Warning | Legacy ASP.NET | Site.master is a WebForms master page | Master pages usually indicate shared WebForms layout structure that may need redesign when moving to modern ASP.NET. |
| Warning | Legacy ASP.NET | Download.ashx is an ASP.NET HTTP handler | HTTP handlers may contain custom request processing behaviour that needs mapping to modern ASP.NET middleware, endpoints, or controllers. |
| Info | Legacy ASP.NET | Global.asax is a Global.asax application file | Global.asax may contain application startup, routing, error handling, or lifecycle code that should be reviewed when migrating to modern ASP.NET hosting. |
| Warning | Legacy ASP.NET | HomeController is an ASP.NET MVC controller | ASP.NET MVC controllers may contain routing, action filters, model binding, authentication, or System.Web-specific behaviour that needs review when moving to ASP.NET Core. |
| Info | Legacy ASP.NET | RouteConfig.cs is an ASP.NET route configuration file | Route configuration may define URL patterns, defaults, constraints, or ignored routes that should be reviewed when migrating to endpoint routing in ASP.NET Core. |
| Info | Legacy ASP.NET | AdminAreaRegistration is an ASP.NET MVC area registration | ASP.NET MVC area registrations may define area-specific routes and feature boundaries that should be reviewed when migrating to ASP.NET Core endpoint routing. |
| Info | Legacy ASP.NET Startup | Global.asax.cs Application_Start contains ASP.NET application startup code | Application_Start may contain route, filter, bundle, dependency injection, error handling, or application lifecycle registration that needs mapping to ASP.NET Core hosting. |
| Info | Legacy ASP.NET Startup | AreaRegistration.RegisterAllAreas registers ASP.NET MVC areas | Area registration calls identify MVC area routing setup that should be reviewed during ASP.NET Core endpoint routing migration. |
| Info | Legacy ASP.NET Routing | RouteConfig.RegisterRoutes registers ASP.NET routes | Route registration calls identify conventional route setup that should be mapped carefully to ASP.NET Core endpoint routing. |
| Warning | Legacy ASP.NET Bundling | BundleConfig.cs is an ASP.NET MVC bundle configuration file | ASP.NET MVC bundling and minification usually need replacement with a modern static asset, build, or bundling strategy. |
| Warning | Legacy ASP.NET Bundling | BundleConfig.RegisterBundles registers ASP.NET MVC bundles | Bundle registration calls may affect CSS and JavaScript delivery and should be reviewed when moving to modern ASP.NET hosting. |
| Warning | Legacy ASP.NET Filters | FilterConfig.cs is an ASP.NET MVC filter configuration file | Global filters can affect authorization, error handling, caching, model binding, or other cross-cutting request behaviour during migration. |
| Warning | Legacy ASP.NET Filters | FilterConfig.RegisterGlobalFilters registers ASP.NET MVC global filters | Global filter registration should be reviewed because equivalent ASP.NET Core filters, middleware, or endpoint conventions may need to be configured explicitly. |
| Warning | Legacy ASP.NET Web API | CustomersApiController is an ASP.NET Web API controller | ASP.NET Web API controllers may contain HTTP API behaviour, routing, model binding, filters, authentication, or System.Web hosting assumptions that need review when moving to ASP.NET Core. |
| Info | Legacy ASP.NET Web API | CustomersApiController.Get is an ASP.NET Web API action | Web API actions identify HTTP endpoint behaviour that should be reviewed for routing, request and response shape, model binding, filters, and ASP.NET Core API migration. |
| Info | Legacy ASP.NET Web API | CustomersApiController.Create is an ASP.NET Web API action | Web API actions identify HTTP endpoint behaviour that should be reviewed for routing, request and response shape, model binding, filters, and ASP.NET Core API migration. |
| Info | Legacy ASP.NET Web API Routing | CustomersApiController [RoutePrefix] uses ASP.NET Web API attribute routing | Web API attribute routes should be mapped carefully to ASP.NET Core endpoint routing to preserve URL patterns, defaults, constraints, and client compatibility. |
| Info | Legacy ASP.NET Web API Routing | CustomersApiController.Get [Route] uses ASP.NET Web API attribute routing | Web API attribute routes should be mapped carefully to ASP.NET Core endpoint routing to preserve URL patterns, defaults, constraints, and client compatibility. |
| Info | Legacy ASP.NET Web API Routing | CustomersApiController.Create [Route] uses ASP.NET Web API attribute routing | Web API attribute routes should be mapped carefully to ASP.NET Core endpoint routing to preserve URL patterns, defaults, constraints, and client compatibility. |
| Warning | Legacy ASP.NET Web API Attributes | CustomersApiController.Get [HttpGet] uses an ASP.NET Web API action attribute | Web API action attributes such as HTTP verb, authorization, anonymous access, and accept verbs attributes may affect endpoint behaviour during ASP.NET Core migration. |
| Warning | Legacy ASP.NET Web API Attributes | CustomersApiController.Create [HttpPost] uses an ASP.NET Web API action attribute | Web API action attributes such as HTTP verb, authorization, anonymous access, and accept verbs attributes may affect endpoint behaviour during ASP.NET Core migration. |
| Info | Legacy ASP.NET Web API | WebApiConfig.cs is an ASP.NET Web API configuration file | WebApiConfig may define HTTP API routing, formatters, filters, services, or other Web API configuration that should be mapped during ASP.NET Core migration. |
| Info | Legacy ASP.NET Web API Routing | MapHttpRoute registers ASP.NET Web API routes | Web API route registration calls identify conventional HTTP API route setup that should be mapped carefully to ASP.NET Core endpoint routing. |
| Info | Legacy ASP.NET Web API Startup | GlobalConfiguration.Configure registers ASP.NET Web API startup configuration | Web API startup registration should be reviewed because routing, formatters, filters, dependency resolution, or other API configuration may need explicit ASP.NET Core equivalents. |
| Info | Configuration | Web.config contains 1 connection string(s) | Connection strings identify external data dependencies that should be reviewed during migration planning. |
```

These hints are intended to guide the first review of a codebase. They should be treated as discovery signals, not final migration advice.

---

## Modernisation Review Summary

LegacyLens.NET can group detailed modernisation hints into higher-level review areas.

This provides a quick “where should I look first?” view while keeping the full `Modernisation Hints` table as supporting evidence.

Current review summary areas include:

- WCF migration
- Legacy ASP.NET migration
- Routing review
- Startup and request pipeline review
- Configuration review
- Dependency review
- Target framework review
- Project dependency review
- Other review

Review areas are ranked by:

- highest discovered severity
- review-area priority
- number of risk hints
- number of warning hints
- number of informational hints
- review area name

Review-area priority is intentionally lightweight. It helps actionable migration areas such as WCF migration and Legacy ASP.NET migration appear above generic baseline findings such as target framework review when they have the same highest severity.

Example report output:

```markdown
## Modernisation Review Summary

| Priority | Review Area | Highest Severity | Risks | Warnings | Info | Summary |
|---:|---|---|---:|---:|---:|---|
| 1 | WCF migration | Risk | 3 | 7 | 8 | 3 risk, 7 warning, 8 info hint(s). Review service boundaries, bindings, security, timeout, payload, metadata, contract, and WCF package usage before choosing a migration approach. |
| 2 | Legacy ASP.NET migration | Risk | 2 | 3 | 8 | 2 risk, 3 warning, 8 info hint(s). Review classic ASP.NET, System.Web, WebForms, ASMX, handlers, MVC, or Web API usage before planning an ASP.NET Core migration. |
| 3 | Target framework review | Risk | 4 | 0 | 0 | 4 risk, 0 warning, 0 info hint(s). Review target frameworks to understand upgrade paths, .NET Framework dependencies, and modern .NET migration constraints. |
| 4 | Startup and request pipeline review | Warning | 0 | 24 | 3 | 0 risk, 24 warning, 3 info hint(s). Review application startup, dependency resolver setup, controller factories, global filters, action attributes, formatters, message handlers, CORS, model binding, value providers, bundling, and cross-cutting request behaviour that may need ASP.NET Core equivalents. |
| 5 | Configuration review | Warning | 0 | 1 | 1 | 0 risk, 1 warning, 1 info hint(s). Review appSettings, connection strings, and custom configuration sections for runtime behaviour and external dependencies. |
| 6 | Dependency review | Warning | 0 | 1 | 2 | 0 risk, 1 warning, 2 info hint(s). Review package dependencies that may affect migration, replacement, compatibility, or framework upgrade planning. |
| 7 | Routing review | Info | 0 | 0 | 10 | 0 risk, 0 warning, 10 info hint(s). Review conventional routes, attribute routes, area routes, and Web API route registrations to preserve URL and client compatibility. |
```

The review summary is intentionally lightweight. It does not replace the detailed hint table; it gives a prioritised overview so developers can decide which areas deserve attention first.

---

## Example Use Cases

LegacyLens.NET can be used when:

- you have inherited a legacy .NET application
- you need to understand a codebase before making changes
- the solution does not build locally
- you need to document project dependencies
- you need to identify which projects are included in one or more solution files
- you want to create diagrams for stakeholders
- you are assessing modernisation effort
- you are preparing for refactoring or migration
- you need to identify legacy WCF configuration and integration points
- you need to identify WCF binding configuration, security, credential, timeout, message size, buffer, transfer mode, reader quota, or metadata exchange usage before planning endpoint migration
- you need to identify WCF service contracts and operations defined in source code
- you need to identify classic ASP.NET artifacts such as WebForms pages, ASMX services, HTTP handlers, custom HTTP module and handler registrations from `web.config`, MVC controllers, MVC actions, MVC route attributes, MVC action attributes, MVC area registrations, Web API controllers, Web API actions, Web API route attributes, Web API action attributes, route configuration, Web API configuration, MVC startup registration, Web API startup registration, bundle configuration, filter configuration, or `Global.asax`
- you need to understand whether a legacy web application contains UI, service, handler, MVC controller, MVC action, Web API controller, Web API action, MVC or Web API attribute routing, MVC or Web API action attributes, area routing, conventional routing, startup registration, dependency resolver setup, controller factory setup, model binder or value provider setup, formatter configuration, message handlers, filters, CORS configuration, bundle configuration, or application lifecycle artifacts that may affect ASP.NET Core migration planning
- you need to identify configuration-heavy applications, connection strings, or custom configuration sections
- you want a prioritised list of modernisation review areas showing where to look first
- you need to identify likely migration risks before deeper analysis

---
