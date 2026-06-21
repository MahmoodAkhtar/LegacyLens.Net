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
- phase-based visual progress feedback during scans so large scans do not appear frozen
- current scan phase messages for major steps such as project discovery, shared file inventory building, solution discovery, WCF/configuration scanning, legacy ASP.NET scanning, modernisation analysis, main report writing, and selected optional artifact generation
- completed phase messages with useful counts once known
- real animated `| / - \` spinner for the actively running phase in normal and verbose interactive console output
- spinner updates on the same console line and clean replacement with completed `✓ ...` messages
- no percentage progress bar for MVP, because the full scan workload is discovered progressively
- `--quiet` suppression of non-essential progress and spinner output
- deterministic non-animated progress when output is redirected or the console is non-interactive
- `--verbose` progress details for useful per-project, per-file, per-phase, or per-artifact diagnostics without corrupting active spinner output
- safe spinner cleanup on scan success or failure
- elapsed scan duration and generated output paths in final console output
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
- shared Markdown-safe table-cell formatting across generated Markdown reports and optional artifacts
- visible, structurally safe evidence table cells for XML/configuration snippets, source-code snippets, paths, pipe characters, newlines, and backticks
- reporting-layer Markdown escaping/inline-code formatting without changing discovery behaviour, analyzer models, or raw evidence values
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
- WCF service contract discovery from project-associated C# source files
- WCF service-contract scanning during normal CLI execution using the shared project-aware file inventory rather than a duplicate recursive full-root `.cs` scan
- WCF service-contract discovery that remains independent of configured WCF endpoints or behaviours, because source-level contracts can exist without configuration endpoints
- WCF service-contract pre-filtering that skips indexed C# files without `ServiceContract` or `ServiceContractAttribute` text before applying heavier interface and operation matching
- WCF operation discovery from `[OperationContract]` and `[OperationContractAttribute]` methods
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
- optional `--artifacts configuration-inventory` command support for producing the configuration-inventory artifact
- optional `--artifacts data-access` command support for producing the data-access artifact
- optional `--artifacts edmx-analysis` command support for producing the edmx-analysis artifact
- optional `--artifacts class-dependencies` command support for producing the class-dependencies artifact
- optional `--artifacts class-dependency-scope` command support for producing a timestamped scoped class dependency artifact when `--class-dependency-type <fully-qualified-type-name>` is supplied
- optional `--artifacts interface-inventory` command support for producing the interface-inventory artifact
- optional `--artifacts solution-topology` command support for producing the solution-topology artifact
- optional `--artifacts code-complexity` command support for producing the code-complexity artifact
- optional `--artifacts <name1,name2>` command support for generating a selected comma-separated subset of optional artifacts
- optional `--artifacts all` command support for generating every supported optional artifact
- case-insensitive artifact name matching
- support for spaces around commas in comma-separated artifact selection
- artifact selection de-duplication so duplicate names do not generate duplicate reports
- clear validation errors for unknown artifact names, including a list of supported values
- validation that `all` cannot be combined with other artifact names
- normal `discovery-report.md` generation regardless of optional artifact selection
- optional `--upgrade-target <tfm>` command support as upgrade report wording context only when selected artifacts include upgrade-readiness, upgrade-blockers, or all; it does not change discovery scope or perform compatibility checks
- optional `--class-dependency-type <fully-qualified-type-name>` command support as scoped class dependency type context only when selected artifacts include class-dependency-scope or all; it does not change normal artifact discovery scope
- validation that class-dependency-scope requires `--class-dependency-type <fully-qualified-type-name>` when explicitly selected
- validation that plain `--artifacts all` does not require a class dependency type and does not generate scoped reports unless a type is supplied
- validation that `--class-dependency-type` is rejected when the selected artifacts do not include class-dependency-scope or all
- static code complexity analysis for identifying C# refactoring and review hotspots using no-build syntax-level cyclomatic complexity estimates
- code-complexity report generation as `code-complexity.md`
- code-complexity analysis over shared `ScanFileInventory.CSharpFiles` without a separate recursive filesystem walk
- code-complexity member-level reporting for methods, constructors, local functions, property accessors, indexer accessors, operator overloads, conversion operators, and top-level statements where practical
- code-complexity aggregation by type, namespace, project, and scan root
- code-complexity severity banding using simple review heuristics such as Low, Moderate, High, and Very High
- code-complexity generated-code indicators where cheaply detectable, without silently hiding generated files unless a shared exclusion convention already applies
- code-complexity notes and limitations explaining static no-build syntax estimation and avoiding claims of exact Microsoft metrics, runtime risk, defect probability, testability, maintainability, correctness, or safe automatic refactoring
- static class dependency analysis for identifying source-level type relationships and coupling concerns
- class-dependencies report generation as `class-dependencies.md`
- class-dependencies discovery of source-defined types such as classes, interfaces, records, structs, and enums where useful
- class-dependencies reporting for constructor parameters, fields, properties, method parameters, return types, local variables, object creation, static member access, inheritance, interface implementations, attributes, and generic type usage
- class-dependencies concern reporting for hardcoded concrete dependencies, direct infrastructure construction, static dependency concerns, concrete field/property dependencies, constructor parameters to concrete classes, inheritance from concrete base classes, framework-specific attributes, and time access where evidence exists
- class-dependencies severity labelling using `High`, `Medium`, and `Low`
- class-dependencies high-coupling hotspot reporting using outgoing dependency count, incoming dependency count, and concern count
- class-dependencies focused Mermaid diagram generation with dependency-kind edge labels
- class-dependencies evidence reporting with project name, source path, line number, source type, target type, dependency kind, and concise source evidence where possible
- class-dependencies notes and limitations explaining static no-build analysis and that findings mean “requires review”, not “proven runtime usage”
- class-dependency-scope report generation using type-specific timestamped filenames such as `class-dependency-scope.SampleLegacyApp.Services.CustomerService.20260620-153045.md`
- class-dependency-scope reporting for a requested fully qualified root type, direct outbound source-level dependencies, direct inbound dependants, related concerns, compact Mermaid diagram, generated local and UTC timestamps, and static analysis limitations
- class-dependency-scope no-match and ambiguity reporting without silently falling back to short-name matching or guessing between duplicate full-name matches
- class-dependency-scope preservation of historical reports by default through timestamped filenames during repeated refactoring runs
- static interface-inventory analysis for identifying available abstractions and likely extension points
- interface-inventory report generation as `interface-inventory.md`
- interface-inventory discovery of interface definitions, implementations, static consumers, generic and collection-based interface usage, inherited interfaces, endpoint delegate parameters, service-locator usage, DI/IoC registration evidence, and visible XML/configuration-driven wiring where discoverable
- interface-inventory reporting for multiple implementations, no static implementation found, no static consumer found, registration evidence found, dynamic wiring that may exist, configuration-driven wiring that may exist, possible extension points, likely roles, and requires-review findings
- interface-inventory notes and limitations explaining static no-build analysis and that findings do not prove runtime usage, active registration, unused interfaces, or completeness
- static solution-topology analysis for onboarding and codebase orientation
- solution-topology report generation as `solution-topology.md`
- solution-topology reporting of solution membership, project relationships, dependency direction, entry-point indicators, configuration concentration, and suggested review boundaries where static evidence exists
- solution-topology notes and limitations explaining static no-build analysis and that the report is orientation evidence, not a proven runtime architecture model
- static configuration-inventory analysis for understanding the visible configuration surface of a legacy .NET codebase
- configuration-inventory report generation as `configuration-inventory.md`
- configuration-inventory discovery of visible configuration files such as `App.config`, `Web.config`, `*.config`, `appsettings.json`, `appsettings.*.json`, `.settings` files, and relevant build/package configuration files where useful
- configuration-inventory reporting for app settings, connection strings, custom configuration sections, environment transforms, WCF configuration, ASP.NET/IIS configuration, binding redirects, authentication and authorization settings, logging/diagnostics configuration, Entity Framework configuration, SMTP/mail settings, configuration API usage where discoverable, and source-code configuration usage mapped back to visible configured keys where possible
- configuration-inventory detection of literal `ConfigurationManager.AppSettings[...]`, `ConfigurationManager.AppSettings.Get(...)`, `ConfigurationManager.ConnectionStrings[...]`, and similar source-code configuration access patterns where possible
- configuration-inventory classification of dynamic, computed, interpolated, concatenated, variable-based, or method-call-based configuration key access as requiring review
- configuration-inventory reconciliation of source-used keys against visible XML and JSON configuration entries without claiming runtime completeness or proving unused-looking keys are genuinely unused
- configuration-inventory sensitive value masking for passwords, API keys, tokens, client secrets, SAS tokens, storage account keys, private feed credentials, and connection string secrets
- configuration-inventory suggested files to review first based on concentration of configuration evidence and migration relevance
- configuration-inventory migration considerations and suggested team questions for upgrade, deployment, onboarding, and environment setup
- configuration-inventory notes and limitations explaining static no-build analysis and that findings mean “requires review”, not “proven runtime behaviour”
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


### Configuration Inventory Artifact

The `configuration-inventory` capability is an MVP-scope addition. It should produce `configuration-inventory.md` as a separate Markdown artifact. It is a static, evidence-backed inventory for understanding visible configuration files, configuration values, configuration sections, settings, transforms, source-code configuration usage, key reconciliation, and migration-relevant configuration concerns before upgrade, deployment, onboarding, or refactoring work starts.

MVP scope:

- Add a `configuration-inventory` capability that can produce `configuration-inventory.md`.
- Use existing project discovery, configuration discovery, and shared file inventory where possible rather than duplicating broad scan logic.
- Keep normal CLI WCF service-contract scanning on the shared file-inventory path so large codebases with no WCF contracts do not pay for an additional independent recursive C# source walk.
- Discover visible configuration files such as `App.config`, `Web.config`, `*.config`, `Web.Debug.config`, `Web.Release.config`, `appsettings.json`, `appsettings.*.json`, `.settings` files, and useful build/package configuration files such as `NuGet.config` where relevant.
- Associate configuration files and configuration findings with discovered projects where possible.
- Report app settings with key names and values where values are discoverable, masking or redacting sensitive parts.
- Report connection strings by name, source file, provider where available, and safe value where useful, without dumping full raw secrets.
- Flatten JSON configuration files into setting-path rows where feasible, for example `ConnectionStrings:RabbitMQ` and `RabbitMQ:HostName`, with sensitive values masked.
- Report custom sections, WCF `system.serviceModel` configuration, ASP.NET/IIS `system.web` and `system.webServer` sections, binding redirects, authentication and authorization settings, logging/diagnostics configuration, Entity Framework configuration, SMTP/mail settings, and configuration API usage where feasible.
- Detect literal `ConfigurationManager.AppSettings[...]`, `ConfigurationManager.AppSettings.Get(...)`, `ConfigurationManager.ConnectionStrings[...]`, `ConfigurationManager.ConnectionStrings.Get(...)`, and similar fully qualified source-code access patterns where possible.
- Record source-code configuration usage with project name, source file, line number, concise evidence, usage kind, literal key where available, resolution, and review status.
- Classify dynamic, computed, interpolated, concatenated, variable-based, or method-call-based configuration key access as requiring review without inventing a key.
- Reconcile literal source-used keys against visible configured entries, distinguishing `Matched visible configuration entry`, `No visible configuration entry found`, `Dynamic key requires review`, and `No static source usage detected`.
- Group detailed report output by project and source file, then by category within each file, so developers can quickly see which setting is in which file.
- Use `Value` as the report column for discovered values with sensitive parts masked where needed.
- Use `N/A`, not `Unknown`, for structural findings that do not have a scalar value.
- Mask or redact sensitive values such as passwords, API keys, tokens, client secrets, SAS tokens, storage account keys, certificate/private-key material, private feed credentials, URI credentials, and connection string secrets.
- Include summary counts, analysis scope, configuration overview, configuration values by source file, source-code configuration usage, configuration key reconciliation, suggested files to review first, migration considerations, suggested questions to ask the team, and notes/limitations.
- Add unit tests for analyzer rules, source-code usage detection, dynamic key classification, source-key reconciliation, cautious no-static-usage wording, project attribution, JSON setting flattening where implemented, masking/redaction, CLI artifact selection, and Markdown output.

Out of scope for MVP:

- Running the application.
- Applying config transforms.
- Validating configuration syntax beyond safe parsing where implemented.
- Validating credentials, certificates, connection strings, or tokens.
- Connecting to configured services or external systems.
- Proving production runtime behaviour.
- Proving a setting is used or unused. Static source usage and no-static-usage findings are review signals only.
- Fully evaluating runtime configuration inheritance.
- Resolving deployment-time substitutions.
- Guaranteeing completeness.
- Exposing full secrets or sensitive values.

### Interface Inventory Artifact

The `interface-inventory` capability is an MVP-scope addition. It should produce `interface-inventory.md` as a separate Markdown artifact. It is a static, evidence-backed interface and extension-point report for understanding available abstractions before implementing new functionality, replacing behaviour, refactoring, testing, or modernising a .NET codebase.

MVP scope:

- Add an `interface-inventory` capability that can produce `interface-inventory.md`.
- Use discovered projects and the shared file inventory to locate `.cs` source files and associate findings with project names where possible.
- Discover C# interface declarations, including name, full name where derivable, namespace, project name, source path, line number, member counts, generic signature or arity, inherited interfaces, marker attributes such as `[ServiceContract]`, visibility where easy to identify, and concise evidence.
- Discover classes, records, and structs implementing interfaces through base lists, including generic interface evidence where useful.
- Discover consumers through constructor parameters, fields, properties, method parameters, return types, local variables, generic type arguments, collection-based interface consumption, inherited interfaces, endpoint delegate parameters, and service-locator or resolver calls where type arguments are visible.
- Discover registration evidence for Microsoft DI, Castle Windsor, Autofac, Ninject, Unity, StructureMap, Simple Injector, LightInject, Lamar, Common Service Locator, ASP.NET MVC/Web API dependency resolver setup, and factory registrations where simple static evidence exists.
- Discover visible XML/configuration-driven IoC evidence for Spring.NET, Castle Windsor XML, Unity XML, Enterprise Library/ObjectBuilder-style configuration, and custom object factory sections where feasible, using only meaningful configuration-bearing elements and attributes as evidence.
- Preserve evidence including project name, source/config path, line number where available, interface name, implementation name where extractable, registration kind, lifetime where extractable, consumer kind, concise snippet, and requires-review flag. Spring.NET evidence snippets should be concise object/property/constructor/factory/alias/parent-style configuration snippets rather than comments, descriptions, root container text, or broad serialized XML.
- Highlight interfaces with multiple implementations, no static implementation found, no static consumer found, dynamic wiring that may exist, configuration-driven wiring that may exist, likely roles, and possible extension points.
- Mark factory-based, reflection-based, container-scanning, XML/configuration-driven, alias-based, parent/child-object, profile-based, service-locator, and otherwise dynamic evidence as requiring review. Spring.NET XML evidence should remain `RequiresReview = true`.
- Add unit tests for interface discovery, implementation mapping, consumer mapping, registration evidence, XML/configuration evidence, dynamic wiring classification, CLI artifact selection, and Markdown output. Add regression tests proving Spring.NET XML comments, `<description>` text, and root `<objects>` text do not create registration evidence, while real object/property evidence still does and assembly-qualified XML type names simplify correctly.

Out of scope for MVP:

- Building the solution.
- Running the application or tests.
- NuGet restore.
- Full semantic compilation analysis.
- Executing container bootstrap code.
- Loading assemblies.
- Applying transforms.
- Resolving runtime dependency injection or proving the runtime object graph.
- Reflection, dynamic loading, runtime factory behaviour, generated code behaviour, or conditional runtime behaviour analysis.
- Proving that an interface is used, unused, active, registered, safe to implement, or complete.

### Class Dependencies Artifact

The `class-dependencies` capability is an MVP-scope addition. It should produce `class-dependencies.md` as a separate Markdown artifact. It is a static, evidence-backed source-level dependency report for understanding class and type coupling before refactoring, testing, or modernising a .NET codebase.

The on-demand `class-dependency-scope` capability is also an MVP-scope addition. It should produce a separate timestamped Markdown artifact for one requested fully qualified type and is intended to help a developer understand a narrow section of a large codebase during refactoring without reading the full `class-dependencies.md` report.

MVP scope:

- Add a `class-dependencies` capability that can produce `class-dependencies.md`.
- Use discovered projects to locate `.cs` source files and associate findings with project names where possible.
- Discover source-defined types such as classes, interfaces, records, structs, and enums where useful, while focusing mainly on classes for the MVP report.
- Detect source-level dependencies from constructor parameters, fields, properties, method parameters, return types, local variables, object creation, static member access, base classes, interface implementations, attributes, and generic type arguments.
- Preserve evidence including project name, source path, line number, source type, target type, dependency kind, and concise source snippet where possible.
- Identify coupling concerns such as hardcoded concrete dependencies, direct infrastructure construction, static dependency concerns, concrete field/property dependencies, constructor parameters to concrete classes, inheritance from concrete base classes, framework-specific attributes, and time access.
- Assign concern severity using `High`, `Medium`, and `Low` with cautious why-it-matters and recommendation text.
- Report top coupled types, hardcoded concrete dependencies, static dependency hotspots, full type dependency inventory, and per-type details where useful.
- Generate a focused Mermaid diagram using dependency-kind edge labels and grouping multiple kinds between the same source and target where practical.
- Add unit tests for analyzer rules, concern classification, Mermaid output, CLI artifact selection, and Markdown output.

Out of scope for MVP:

- Building the solution.
- Running the application or tests.
- NuGet restore.
- Full semantic compilation analysis.
- Runtime dependency injection resolution.
- Reflection, dynamic loading, factory behaviour, generated code behaviour, or conditional runtime behaviour analysis.
- Runtime call graphs.
- Proving that a dependency is always used or unused at runtime.

### Scoped Class Dependency Artifact

The `class-dependency-scope` capability is an MVP-scope addition. It should produce a focused, timestamped Markdown artifact for one requested fully qualified type. It answers: “For this specific type, what does it directly depend on, what directly depends on it, and what review concerns involve it?”

MVP scope:

- Add a `class-dependency-scope` artifact name and a `--class-dependency-type <fully-qualified-type-name>` CLI option.
- Require `--class-dependency-type` when `class-dependency-scope` is explicitly selected.
- Allow `--artifacts all --class-dependency-type <type>` to generate all normal artifacts plus the scoped report.
- Keep plain `--artifacts all` valid without a scoped type and do not generate scoped reports in that case.
- Reject `--class-dependency-type` when selected artifacts do not include `class-dependency-scope` or `all`.
- Reuse the existing no-build `ClassDependencyAnalyzer` and `ScanContext.FileInventory`; do not create a second dependency scanner.
- Resolve the requested fully qualified type name case-insensitively against `DiscoveredType.FullName`.
- Do not silently fall back to short-name matching.
- Report no-match and ambiguity cases clearly with source files analysed, discovered type counts, project/source-path evidence where available, and generated timestamp metadata.
- Include direct outbound dependencies where the root type is the source.
- Include direct inbound dependants where the root type is the target.
- Include concerns where the root type is either source or target.
- Include a compact Mermaid diagram centred on the root type.
- Write each report to `class-dependency-scope.<safe-fully-qualified-type-name>.<yyyyMMdd-HHmmss>.md` using a local sortable filename timestamp.
- Include both local and UTC generated timestamps inside the report body.
- Preserve historical reports by default so repeated refactoring runs for the same type do not overwrite earlier files.

Out of scope for MVP:

- Runtime dependency injection resolution.
- Reflection or dynamic loading analysis.
- Transitive dependency completeness.
- Runtime call graph generation.
- Proving runtime usage.
- Treating simple-name matches as semantically certain when multiple source-defined types share the same short name.

## MVP Exit Criteria

The MVP should be considered complete when the tool can produce a useful static discovery report for the sample legacy solution without requiring that solution to build.

The MVP exit criteria are:

- The CLI can scan the sample legacy solution successfully.
- The generated Markdown report includes solution, project, target framework, package reference, package compatibility review, assembly reference, project reference, WCF, Legacy ASP.NET, configuration, modernisation hint, and modernisation review summary sections. The MVP can also produce separate `upgrade-readiness-report.md`, `upgrade-blockers.md`, `external-dependencies.md`, `configuration-inventory.md`, `data-access-inventory.md`, `edmx-analysis.md`, `class-dependencies.md`, `interface-inventory.md`, and `solution-topology.md` artifacts.
- The report identifies the main modernisation review areas clearly enough for a developer to decide where to investigate first.
- The package compatibility review shows package name, version where available, project target framework, package target framework where available, source format, source path, and possible compatibility concern without claiming to perform full NuGet compatibility resolution.
- Modernisation hints include useful evidence metadata where a clear source exists, including evidence kind, evidence name, confidence, source path, and reason.
- The report does not contain known duplicated, misleading, or materially low-value findings that would confuse a reader.
- Generated Markdown tables remain structurally valid and evidence remains visible in rendered previews, including XML-like evidence such as Spring.NET `<object ... />` registration snippets.
- Existing automated tests pass.
- The upgrade-readiness report includes current project targets, project-level readiness classifications, possible upgrade concerns, package upgrade considerations, assembly reference considerations, configuration/runtime considerations, and clear static-analysis limitations.
- The upgrade-blockers report includes a blocker overview, grouped blocker details, impact labels, evidence, why each blocker matters, decisions required, suggested review order, and clear static-analysis limitations.
- The data-access inventory includes an analysis scope, data access overview, projects with data access indicators, connection string/provider information with masked sensitive values, ORM and data access technology evidence, suggested files to review first, migration considerations, suggested team questions, and clear static-analysis limitations.
- The external-dependencies report includes an analysis scope, dependency overview, grouped dependency sections, source/evidence details, confirmation flags, suggested team questions, sensitive value masking, and clear static-analysis limitations.
- The configuration-inventory report includes an analysis scope, configuration overview, grouped configuration values by source file, source-code configuration usage, configuration key reconciliation, dynamic key review signals, cautious no-static-usage wording, sensitive value masking, and clear static-analysis limitations.
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
