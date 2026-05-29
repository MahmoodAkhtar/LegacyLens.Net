# LegacyLens.NET

LegacyLens.NET is a static discovery tool for unfamiliar, legacy, and modern .NET codebases.

It helps developers quickly understand the structure of a .NET solution by scanning solution files, project files, C# source files, and configuration files, then reporting useful information such as solutions, projects, target frameworks, project references, assembly references, package references, WCF endpoint configuration, WCF binding, security, timeout, message size, buffer, transfer mode, and reader quota details, WCF service contracts, service-related configuration, general configuration file usage, and basic modernisation hints.

The aim is to help a developer who is new to a codebase answer questions such as:

- What projects exist in this solution?
- Which target frameworks are being used?
- Which projects depend on each other?
- Which framework assembly references are used by each project?
- Which NuGet packages are referenced?
- Are there signs of legacy technologies such as WCF?
- Which WCF endpoints are configured?
- Which WCF binding configurations, security modes, timeout settings, message size limits, buffer limits, transfer modes, or reader quotas are configured?
- Which WCF service contracts and operations are defined in the source code?
- What configuration files, settings, connection strings, or custom sections exist?
- What modernisation risks or review areas should be looked at first?
- What diagrams or reports can help explain the system to others?

LegacyLens.NET is designed to work through static analysis, meaning it can provide useful information even when the target solution cannot currently be built.

---

## Current Status

LegacyLens.NET is currently in early MVP development.

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
- configuration files from `app.config` and `web.config`
- `appSettings` entry counts
- `connectionStrings` entry counts
- custom configuration section counts from `configSections`
- WCF service contracts from C# source files
- WCF operations marked with `[OperationContract]`
- basic modernisation hints for legacy target frameworks, WCF usage, selected packages, legacy ASP.NET / `System.Web` usage, higher project coupling, selected WCF binding types, WCF security-related endpoint details, WCF timeout settings, WCF message size and buffer limits, WCF transfer modes, WCF reader quotas, metadata exchange endpoints, and configuration-heavy applications

Package discovery behaviour is covered by tests for `<PackageReference />`, `packages.config`, duplicate package handling, and invalid `packages.config` handling.

It can also generate a Markdown discovery report at:

```text
output/discovery-report.md
```

The generated report currently includes:

- a summary of discovered solutions, projects, project references, assembly references, package references, WCF endpoints, and WCF service contracts
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
- WCF service contract and operation information
- configuration file information, including `appSettings`, `connectionStrings`, and custom configuration section counts
- modernisation hints with severity, area, finding, and reason, including Legacy ASP.NET hints when `System.Web` assembly references are found

Example console output:

```text
Projects discovered:
- SampleLegacyApp.Contracts
  Target framework: net48

- SampleLegacyApp.Data
  Target framework: net48
  Package reference: Dapper
  Package reference: EntityFramework

- SampleLegacyApp.Services
  Target framework: net48
  Project reference: ..\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj
  Project reference: ..\SampleLegacyApp.Contracts\SampleLegacyApp.Contracts.csproj

- SampleLegacyApp.Web
  Target framework: net48
  Project reference: ..\SampleLegacyApp.Services\SampleLegacyApp.Services.csproj
  Project reference: ..\SampleLegacyApp.Contracts\SampleLegacyApp.Contracts.csproj
  Assembly reference: System.Web
  Assembly reference: System.Web.Mvc
  Package reference: System.ServiceModel.Http
  Package reference: Newtonsoft.Json

WCF endpoints discovered:
- SampleLegacyApp.Services.CustomerService
  Address:
  Binding: basicHttpBinding
  Contract: SampleLegacyApp.Contracts.ICustomerService
  Config file: C:\Path\To\LegacyLens.Net\samples\SampleLegacyApp\SampleLegacyApp.Web\Web.config

WCF service contracts discovered:
- ICustomerService
  Source file: C:\Path\To\LegacyLens.Net\samples\SampleLegacyApp\SampleLegacyApp.Contracts\CustomerContracts.cs
  Operation: GetCustomer

Configuration files discovered:
- C:\Path\To\LegacyLens.Net\samples\SampleLegacyApp\SampleLegacyApp.Web\Web.config
  App settings: 0
  Connection strings: 0
  Custom sections: 0

Modernisation hints discovered:
- [Risk] Target Framework: SampleLegacyApp.Contracts targets net48
- [Risk] Target Framework: SampleLegacyApp.Data targets net48
- [Risk] Target Framework: SampleLegacyApp.Services targets net48
- [Risk] Target Framework: SampleLegacyApp.Web targets net48
- [Warning] Packages: SampleLegacyApp.Data references EntityFramework
- [Risk] Packages: SampleLegacyApp.Web references System.ServiceModel.Http
- [Info] Packages: SampleLegacyApp.Web references Newtonsoft.Json
- [Risk] WCF: 1 WCF endpoint(s) discovered
- [Warning] WCF Binding: basicHttpBinding endpoint discovered for SampleLegacyApp.Services.CustomerService
- [Risk] WCF: 1 WCF service contract(s) discovered
- [Risk] Legacy ASP.NET: SampleLegacyApp.Web references System.Web
- [Warning] Legacy ASP.NET: SampleLegacyApp.Web references System.Web.Mvc

Solutions discovered:
- SampleLegacyApp
  Solution file: C:\Path\To\LegacyLens.Net\samples\SampleLegacyApp\SampleLegacyApp.sln
  Projects: 4

Markdown report generated: C:\Path\To\LegacyLens.Net\output\discovery-report.md
```

If no solutions, WCF endpoints, WCF service contracts, configuration files, or modernisation hints are found, the console output shows:

```text
WCF endpoints discovered:
- None

WCF service contracts discovered:
- None

Configuration files discovered:
- None

Modernisation hints discovered:
- None

Solutions discovered:
- None
```

---

## Why LegacyLens.NET?

Legacy .NET systems are often difficult to understand because the original developers may no longer be available, documentation may be missing, and the solution may not build cleanly on a modern machine.

LegacyLens.NET aims to make that first investigation easier by producing clear, structured information from the source code itself.

It is especially useful for:

- developers joining an unfamiliar codebase
- contractors starting a legacy .NET assignment
- teams planning modernisation work
- architects reviewing project dependencies
- developers preparing documentation or diagrams
- codebase discovery before refactoring or migration

---

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
- WCF configuration files
- WCF endpoint binding configuration names
- WCF endpoint security modes and credential types
- WCF endpoint timeout settings
- WCF endpoint message size and buffer limits
- WCF endpoint transfer modes
- WCF endpoint reader quota settings
- WCF metadata exchange endpoint indicators
- WCF `[ServiceContract]` interfaces
- WCF `[OperationContract]` methods
- project references
- assembly references
- package references

This makes it useful for old or broken solutions where restoring packages, installing SDKs, or compiling the code may not be possible immediately.

> Note: package reference discovery currently supports both SDK-style `<PackageReference />` entries in `.csproj` files and legacy `packages.config` files located alongside project files. Invalid or unreadable `packages.config` files are ignored so discovery can continue.

> Note: assembly reference discovery currently supports `<Reference Include="..." />` entries in `.csproj` files. Version metadata is removed so references such as `System.Web.Mvc, Version=5.2.9.0` are reported as `System.Web.Mvc`.

> Note: configuration file discovery currently supports `app.config` and `web.config` files. Invalid or unreadable configuration files are ignored so discovery can continue.

> Note: WCF endpoint discovery currently reads configured service endpoints from `app.config` and `web.config` files. Where endpoints reference named binding configurations, LegacyLens.NET also attempts to resolve related security mode, transport credential type, message credential type, timeout, message size, buffer, transfer mode, and reader quota details from the matching binding configuration.

> Note: solution discovery currently supports `.sln` files and extracts referenced C# project paths from project entries. Non-C# project entries and solution folders are ignored.

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
- legacy `packages.config` files located in the same folder as the project file

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

Package names discovered from both sources are merged into the project package reference list. Duplicate package names are removed case-insensitively.

This helps LegacyLens.NET identify important legacy dependencies even when older .NET Framework projects do not use SDK-style package references.

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

This helps identify applications where important runtime behaviour or migration concerns may be hidden in configuration files.

---

## Repository Structure

```text
LegacyLens.Net/
├── artifacts/
├── docs/
│   └── mvp.md
├── output/
├── reports/
├── samples/
│   └── SampleLegacyApp/
├── src/
│   ├── LegacyLens.Cli/
│   ├── LegacyLens.Core/
│   └── LegacyLens.Reporting/
└── tests/
```

---

## Main Projects

| Project | Purpose |
|---|---|
| `LegacyLens.Cli` | Command-line entry point for running scans |
| `LegacyLens.Core` | Core discovery and analysis logic |
| `LegacyLens.Reporting` | Report generation functionality |
| `SampleLegacyApp` | Sample legacy-style .NET application used for testing discovery features |

---

## LegacyLens.Core Structure

The core project is organised around discovery and analysis concepts.

```text
LegacyLens.Core/
├── Abstractions/
├── Analysis/
├── Configuration/
├── Dependencies/
├── Discovery/
├── Models/
└── Wcf/
```

### Abstractions

Contains shared interfaces used by the core discovery and reporting components.

Examples:

- `IScanner`
- `IReportWriter`

### Analysis

Responsible for turning discovered facts into basic review and modernisation hints.

Current analysis work includes:

- modelling modernisation hints
- classifying hints by severity: `Info`, `Warning`, and `Risk`
- identifying old .NET Framework target frameworks such as `net48`
- identifying missing target framework declarations
- identifying WCF-related package usage such as `System.ServiceModel.*`
- identifying classic Entity Framework package usage
- identifying `Newtonsoft.Json` usage as an informational review item
- identifying legacy ASP.NET indicators from `System.Web` assembly references
- identifying `System.Web.*` assembly references as legacy ASP.NET review items
- highlighting projects with several direct project references
- highlighting discovered WCF endpoints
- highlighting selected WCF binding types such as `basicHttpBinding`, `netTcpBinding`, `wsHttpBinding`, and `netMsmqBinding`
- highlighting WCF endpoints with missing binding information
- highlighting WCF endpoints that use named binding configurations
- highlighting WCF endpoint security modes
- highlighting WCF transport credential types
- highlighting WCF timeout settings
- highlighting WCF message size and buffer limits
- highlighting WCF transfer modes, including streaming transfer modes
- highlighting WCF reader quota settings
- highlighting WCF metadata exchange endpoints
- highlighting discovered WCF service contracts
- identifying configuration-heavy applications from `app.config` and `web.config`
- identifying large `appSettings` usage
- identifying connection strings as external data dependency indicators
- identifying custom configuration sections as migration review items

### Configuration

Responsible for detecting useful information from `.config` files.

Current configuration work includes:

- scanning `app.config` and `web.config` files
- counting `appSettings` entries
- counting `connectionStrings` entries
- counting custom configuration sections from `configSections`
- modelling discovered configuration file details such as file path, app setting count, connection string count, and custom section count

### Discovery

Responsible for finding projects, solutions, and source files.

Current discovery work includes:

- solution discovery from `.sln` files
- discovered solution modelling
- project discovery from `.csproj` files
- source file discovery
- discovered project modelling
- package reference discovery from `<PackageReference />` entries
- package reference discovery from legacy `packages.config` files
- assembly reference discovery from `<Reference />` entries

### Dependencies

Responsible for scanning dependency information.

Current dependency work includes:

- project reference scanning
- package reference scanning
- assembly reference scanning

### Models

Contains shared models used to represent scan results, projects, solutions, and dependencies.

### WCF

Responsible for detecting WCF-related code and configuration.

Current WCF work includes:

- scanning `app.config` and `web.config` files
- detecting `<system.serviceModel>` configuration
- extracting configured WCF endpoints
- modelling WCF endpoint details such as service name, address, binding, binding configuration, behaviour configuration, security mode, transport credential type, message credential type, timeout settings, message size limits, buffer limits, transfer mode, reader quota settings, metadata exchange endpoint indicator, contract, and config file path
- scanning C# source files for WCF service contracts
- detecting interfaces marked with `[ServiceContract]`
- detecting operations marked with `[OperationContract]`
- modelling WCF service contract details such as contract name, source file path, and operation names

Planned WCF work includes:

- richer WCF endpoint analysis beyond the currently detected binding, security, credential, timeout, size, transfer mode, reader quota, and metadata exchange hints
- more detailed WCF-related risk and modernisation indicators
- improved service contract parsing for more complex C# syntax

---

## LegacyLens.Reporting Structure

The reporting project is responsible for producing human-readable output from discovered codebase information.

Current reporting work includes:

```text
LegacyLens.Reporting/
├── Html/
├── Markdown/
└── Mermaid/
```

### Markdown

Currently implemented.

Generates:

```text
output/discovery-report.md
```

The Markdown report currently includes:

- summary counts
- discovered solutions
- discovered projects
- target frameworks
- target framework summary grouped by discovered target framework
- package reference summary grouped by discovered package
- project dependency diagram
- project references
- assembly references
- package references
- WCF endpoint details, including binding configuration, security mode, transport credential type, message credential type, metadata exchange indicator, contract, and config file path
- WCF binding details, including timeout settings, message size limits, buffer limits, and transfer mode
- WCF reader quota details
- WCF service contract details
- WCF operation names
- configuration file details
- `appSettings`, `connectionStrings`, and custom configuration section counts
- modernisation hints

### Mermaid

Currently implemented.

Generates a Mermaid project dependency diagram from discovered project references and includes it in the Markdown discovery report.

The diagram is generated from `<ProjectReference />` entries found in `.csproj` files.

Example:

```mermaid
graph TD
    SampleLegacyApp_Web --> SampleLegacyApp_Services
    SampleLegacyApp_Services --> SampleLegacyApp_Data
    SampleLegacyApp_Services --> SampleLegacyApp_Contracts
    SampleLegacyApp_Web --> SampleLegacyApp_Contracts
```

Project names are sanitized for Mermaid output by replacing characters such as `.`, `-`, and spaces with `_`.

### HTML

Planned.

This may later be used to generate richer browser-based reports.

---

## Running the Tool

From the repository root, run:

```powershell
dotnet run --project src/LegacyLens.Cli -- .\samples\SampleLegacyApp\
```

Example:

```powershell
PS C:\Users\YourName\RiderProjects\LegacyLens.Net> dotnet run --project src/LegacyLens.Cli -- .\samples\SampleLegacyApp\
```

This scans the sample application, prints discovered solution, project, assembly reference, WCF, configuration file, and modernisation hint information to the console, and generates a Markdown report at:

```text
output/discovery-report.md
```

Example final console line:

```text
Markdown report generated: C:\Path\To\LegacyLens.Net\output\discovery-report.md
```

---

## Sample Console Output

```text
Projects discovered:
- SampleLegacyApp.Contracts
  Target framework: net48

- SampleLegacyApp.Data
  Target framework: net48
  Package reference: Dapper
  Package reference: EntityFramework

- SampleLegacyApp.Services
  Target framework: net48
  Project reference: ..\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj
  Project reference: ..\SampleLegacyApp.Contracts\SampleLegacyApp.Contracts.csproj

- SampleLegacyApp.Web
  Target framework: net48
  Project reference: ..\SampleLegacyApp.Services\SampleLegacyApp.Services.csproj
  Project reference: ..\SampleLegacyApp.Contracts\SampleLegacyApp.Contracts.csproj
  Assembly reference: System.Web
  Assembly reference: System.Web.Mvc
  Package reference: System.ServiceModel.Http
  Package reference: Newtonsoft.Json

WCF endpoints discovered:
- SampleLegacyApp.Services.CustomerService
  Address:
  Binding: basicHttpBinding
  Contract: SampleLegacyApp.Contracts.ICustomerService
  Config file: C:\Path\To\LegacyLens.Net\samples\SampleLegacyApp\SampleLegacyApp.Web\Web.config

WCF service contracts discovered:
- ICustomerService
  Source file: C:\Path\To\LegacyLens.Net\samples\SampleLegacyApp\SampleLegacyApp.Contracts\CustomerContracts.cs
  Operation: GetCustomer

Configuration files discovered:
- C:\Path\To\LegacyLens.Net\samples\SampleLegacyApp\SampleLegacyApp.Web\Web.config
  App settings: 0
  Connection strings: 0
  Custom sections: 0

Modernisation hints discovered:
- [Risk] Target Framework: SampleLegacyApp.Contracts targets net48
- [Risk] Target Framework: SampleLegacyApp.Data targets net48
- [Risk] Target Framework: SampleLegacyApp.Services targets net48
- [Risk] Target Framework: SampleLegacyApp.Web targets net48
- [Warning] Packages: SampleLegacyApp.Data references EntityFramework
- [Risk] Packages: SampleLegacyApp.Web references System.ServiceModel.Http
- [Info] Packages: SampleLegacyApp.Web references Newtonsoft.Json
- [Risk] WCF: 1 WCF endpoint(s) discovered
- [Warning] WCF Binding: basicHttpBinding endpoint discovered for SampleLegacyApp.Services.CustomerService
- [Risk] WCF: 1 WCF service contract(s) discovered
- [Risk] Legacy ASP.NET: SampleLegacyApp.Web references System.Web
- [Warning] Legacy ASP.NET: SampleLegacyApp.Web references System.Web.Mvc

Solutions discovered:
- SampleLegacyApp
  Solution file: C:\Path\To\LegacyLens.Net\samples\SampleLegacyApp\SampleLegacyApp.sln
  Projects: 4

Markdown report generated: C:\Path\To\LegacyLens.Net\output\discovery-report.md
```

---

## Generated Report Output

LegacyLens.NET currently generates a Markdown report at:

```text
output/discovery-report.md
```

The current report sections are:

- Summary
- Solutions
- Projects
- Target Framework Summary
- Package Reference Summary
- Project Dependency Diagram
- Project References
- Assembly References
- Package References
- WCF Endpoints
- WCF Binding Details
- WCF Reader Quotas
- WCF Service Contracts
- Configuration Files
- Modernisation Hints

The report currently includes sections such as:

````markdown
# LegacyLens.NET Discovery Report

## Summary

- Solutions discovered: 1
- Projects discovered: 4
- Project references discovered: 4
- Package references discovered: 4
- WCF endpoints discovered: 1
- WCF service contracts discovered: 1
- Assembly references discovered: 2

## Solutions

| Solution | Projects | Solution File |
|---|---:|---|
| SampleLegacyApp | 4 | `C:\Path\To\LegacyLens.Net\samples\SampleLegacyApp\SampleLegacyApp.sln` |

## Projects

| Project | Target Framework | Project File |
|---|---|---|

## Target Framework Summary

| Target Framework | Projects |
|---|---:|
| net48 | 4 |

## Package Reference Summary

| Package | Projects |
|---|---:|
| Dapper | 1 |
| EntityFramework | 1 |
| Newtonsoft.Json | 1 |
| System.ServiceModel.Http | 1 |

## Project Dependency Diagram

```mermaid
graph TD
    SampleLegacyApp_Web --> SampleLegacyApp_Services
    SampleLegacyApp_Services --> SampleLegacyApp_Data
    SampleLegacyApp_Services --> SampleLegacyApp_Contracts
    SampleLegacyApp_Web --> SampleLegacyApp_Contracts
```

## Project References

| From | To |
|---|---|

## Assembly References

| Project | Assembly |
|---|---|
| SampleLegacyApp.Web | `System.Web` |
| SampleLegacyApp.Web | `System.Web.Mvc` |

## Package References

| Project | Package |
|---|---|

## WCF Endpoints

| Service | Address | Binding | Binding Configuration | Security Mode | Transport Credential | Message Credential | Metadata Exchange | Contract | Config File |
|---|---|---|---|---|---|---|---|---|---|
| SampleLegacyApp.Services.CustomerService |  | basicHttpBinding | CustomerBinding | Transport | Windows | UserName | True | SampleLegacyApp.Contracts.ICustomerService | `C:\Path\To\LegacyLens.Net\samples\SampleLegacyApp\SampleLegacyApp.Web\Web.config` |

## WCF Binding Details

| Service | Binding | Binding Configuration | Open Timeout | Close Timeout | Send Timeout | Receive Timeout | Max Received Message Size | Max Buffer Size | Max Buffer Pool Size | Transfer Mode |
|---|---|---|---|---|---|---|---:|---:|---:|---|
| SampleLegacyApp.Services.CustomerService | basicHttpBinding | CustomerBinding | 00:01:00 | 00:01:00 | 00:02:00 | 00:10:00 | 1048576 | 65536 | 524288 | Streamed |

## WCF Reader Quotas

| Service | Binding | Binding Configuration | Max Depth | Max String Content Length | Max Array Length | Max Bytes Per Read | Max Name Table Char Count |
|---|---|---|---:|---:|---:|---:|---:|
| SampleLegacyApp.Services.CustomerService | basicHttpBinding | CustomerBinding | 32 | 8192 | 16384 | 4096 | 16384 |

## WCF Service Contracts

| Contract | Operations | Source File |
|---|---|---|
| ICustomerService | GetCustomer | `C:\Path\To\LegacyLens.Net\samples\SampleLegacyApp\SampleLegacyApp.Contracts\CustomerContracts.cs` |

## Configuration Files

| Config File | App Settings | Connection Strings | Custom Sections |
|---|---:|---:|---:|
| `C:\Path\To\LegacyLens.Net\samples\SampleLegacyApp\SampleLegacyApp.Web\Web.config` | 0 | 0 | 0 |

## Modernisation Hints

| Severity | Area | Finding | Reason |
|---|---|---|---|
| Risk | Packages | SampleLegacyApp.Web references System.ServiceModel.Http | System.ServiceModel packages indicate WCF-related usage, which is important for modernisation planning. |
| Risk | Target Framework | SampleLegacyApp.Web targets net48 | .NET Framework projects usually need extra assessment before migration to modern .NET. |
| Risk | WCF | 1 WCF endpoint(s) discovered | Configured WCF endpoints usually represent service boundaries or integration points that need migration assessment. |
| Risk | WCF | 1 WCF service contract(s) discovered | WCF service contracts identify service APIs that may need redesign, replacement, or compatibility planning. |
| Risk | Legacy ASP.NET | SampleLegacyApp.Web references System.Web | System.Web usually indicates classic ASP.NET, WebForms, MVC 5, ASMX, or ASP.NET-hosted legacy functionality that does not directly migrate to modern ASP.NET Core. |
| Warning | Legacy ASP.NET | SampleLegacyApp.Web references System.Web.Mvc | System.Web-related assemblies indicate legacy ASP.NET functionality that may need separate migration assessment. |
| Warning | Packages | SampleLegacyApp.Data references EntityFramework | Classic Entity Framework may require assessment before migration to EF Core or modern .NET. |
| Warning | WCF Binding | basicHttpBinding endpoint discovered for SampleLegacyApp.Services.CustomerService | basicHttpBinding commonly indicates SOAP interoperability that may need replacement or compatibility planning. |
| Warning | WCF Security | SampleLegacyApp.Services.CustomerService uses WCF security mode Transport | WCF security settings need explicit review when replacing WCF endpoints with modern HTTP, JSON, gRPC, or other service endpoints. |
| Warning | WCF Security | SampleLegacyApp.Services.CustomerService uses transport credential type Windows | Transport credential settings may affect authentication and hosting choices during service migration. |
| Warning | WCF Transfer Mode | SampleLegacyApp.Services.CustomerService uses WCF transfer mode Streamed | Streaming WCF transfer modes may affect endpoint redesign, request buffering, file upload/download behaviour, hosting limits, and client compatibility. |
| Warning | WCF Reader Quotas | SampleLegacyApp.Services.CustomerService has explicit WCF reader quota settings | Configured WCF reader quotas may affect XML payload compatibility, maximum object graph depth, string sizes, array sizes, and generated SOAP client behaviour during migration. |
| Info | WCF Timeout | SampleLegacyApp.Services.CustomerService has explicit WCF timeout settings | Configured WCF timeout values should be reviewed when replacing endpoints because modern HTTP, JSON, gRPC, hosting, gateway, and client timeout behaviour may differ. |
| Info | WCF Binding Limits | SampleLegacyApp.Services.CustomerService has explicit WCF message size or buffer limits | Configured WCF message size and buffer limits should be reviewed when migrating endpoints because equivalent request, response, and hosting limits may need to be set explicitly. |
| Info | WCF Configuration | SampleLegacyApp.Services.CustomerService uses binding configuration CustomerBinding | Named WCF binding configurations may contain security, timeout, size, protocol, or credential settings that need migration review. |
| Info | WCF Metadata | SampleLegacyApp.Services.CustomerService exposes a metadata exchange endpoint | Metadata exchange endpoints are useful discovery signals when identifying SOAP contracts and generated client dependencies. |
| Info | Packages | SampleLegacyApp.Web references Newtonsoft.Json | This is common in legacy and modern projects, but may be reviewed during modernisation. |
````

The generated report is intended to be readable in source control, Markdown preview tools, and documentation systems.

---

## Mermaid Dependency Diagram

LegacyLens.NET includes a Mermaid project dependency diagram in the generated Markdown report.

The diagram is created from discovered project-to-project references and is intended to make the structure of the solution easier to understand visually.

Example:

```mermaid
graph TD
    SampleLegacyApp_Web --> SampleLegacyApp_Services
    SampleLegacyApp_Services --> SampleLegacyApp_Data
    SampleLegacyApp_Services --> SampleLegacyApp_Contracts
    SampleLegacyApp_Web --> SampleLegacyApp_Contracts
```

This makes it easier to visually understand project-to-project relationships.

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

## WCF Service Contract Discovery

LegacyLens.NET can also detect WCF service contracts from C# source files.

The current WCF service contract scanner looks for interfaces marked with `[ServiceContract]` and operations marked with `[OperationContract]`.

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

This helps identify service boundaries defined in code, even when the target solution cannot be built or run.

Current service contract discovery is intentionally lightweight and static. It is based on source scanning rather than compilation.

---

## Modernisation Hint Analysis

LegacyLens.NET can produce basic modernisation hints from the information it discovers.

The current modernisation hint analysis is intentionally lightweight. It does not attempt to fully assess migration effort, but it highlights useful review areas for developers investigating a legacy or unfamiliar .NET codebase.

Current hint areas include:

- target framework review
- project dependency review
- package review
- legacy ASP.NET / `System.Web` review
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
- configuration-heavy application review

Current severity levels are:

| Severity | Meaning |
|---|---|
| `Info` | Useful information to review during discovery |
| `Warning` | Something that may need extra attention |
| `Risk` | Something likely to affect modernisation or migration planning |

Current WCF binding hints include:

| Binding | Severity | Meaning |
|---|---|---|
| Missing binding | `Warning` | The endpoint cannot be fully assessed because binding information is missing |
| `basicHttpBinding` | `Warning` | Commonly indicates SOAP interoperability that may need replacement or compatibility planning |
| `wsHttpBinding` | `Warning` | May indicate SOAP and WS-* features that need modernisation assessment |
| `netTcpBinding` | `Risk` | WCF-specific communication that usually needs careful migration or replacement planning |
| `netMsmqBinding` | `Risk` | Queue-based WCF integration that needs separate migration planning |


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


Current legacy ASP.NET hints include:

| Indicator | Severity | Meaning |
|---|---|---|
| `System.Web` assembly reference | `Risk` | Usually indicates classic ASP.NET, WebForms, MVC 5, ASMX, or ASP.NET-hosted legacy functionality that does not directly migrate to modern ASP.NET Core |
| `System.Web.*` assembly reference | `Warning` | Indicates legacy ASP.NET-related functionality that may need separate migration assessment |

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
| Risk | Packages | SampleLegacyApp.Web references System.ServiceModel.Http | System.ServiceModel packages indicate WCF-related usage, which is important for modernisation planning. |
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
| Warning | Legacy ASP.NET | SampleLegacyApp.Web references System.Web.Mvc | System.Web-related assemblies indicate legacy ASP.NET functionality that may need separate migration assessment. |
| Info | Configuration | Web.config contains 1 connection string(s) | Connection strings identify external data dependencies that should be reviewed during migration planning. |
```

These hints are intended to guide the first review of a codebase. They should be treated as discovery signals, not final migration advice.

---

## MVP Functionality

Current MVP functionality includes:

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
- Markdown discovery report generation
- target framework summary reporting in the generated Markdown report
- package reference summary reporting in the generated Markdown report
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
- WCF service contract discovery from C# source files
- WCF operation discovery from `[OperationContract]` methods
- WCF service contract reporting
- configuration file discovery from `app.config` and `web.config`
- `appSettings`, `connectionStrings`, and custom configuration section counting
- configuration file reporting in the generated Markdown report
- basic modernisation hint analysis
- modernisation hints for old .NET Framework target frameworks
- modernisation hints for missing target framework declarations
- modernisation hints for selected legacy or review-worthy packages
- modernisation hints for legacy ASP.NET and `System.Web` assembly references
- modernisation hints for WCF endpoints, selected WCF binding types, endpoint binding configurations, security modes, transport credential types, timeout settings, message size and buffer limits, transfer modes, reader quotas, metadata exchange endpoints, and service contracts
- modernisation hints for configuration-heavy applications
- modernisation hint reporting in the generated Markdown report
- output file generation under the `output/` directory

Planned MVP features include:

- improved service contract parsing for more complex C# syntax
- additional legacy ASP.NET indicators beyond the current `System.Web` and `System.Web.*` assembly reference hints
- improved severity classification as more discovery signals are added

---

## Development Roadmap

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
- Include configuration file details
- Include WCF binding detail sections
- Include WCF reader quota sections

### Step 3: Dependency diagram generation

Status: Implemented

- Generate Mermaid dependency graph
- Include graph in Markdown report

### Step 4: WCF configuration and service contract discovery

Status: Partially implemented

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
- Detect WCF metadata exchange endpoints from `IMetadataExchange` contracts and `mex*` bindings
- Detect WCF service contracts from C# source files
- Detect WCF operations marked with `[OperationContract]`
- Report contract name, operation names, and source file path

Remaining work:

- Improve endpoint analysis beyond the currently detected binding, security, credential, timeout, size, transfer mode, reader quota, and metadata exchange hints
- Improve service contract parsing for more complex C# syntax

### Step 5: Risk and modernisation hints

Status: Partially implemented

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
- Identify legacy ASP.NET indicators from `System.Web` and `System.Web.*` assembly references
- Identify configuration-heavy application indicators from `app.config` and `web.config`
- Identify large `appSettings` usage
- Identify connection strings as external data dependency indicators
- Identify custom configuration sections
- Include modernisation hints in the generated Markdown report

Remaining work:

- Add more legacy ASP.NET indicators beyond the current `System.Web` and `System.Web.*` assembly reference hints
- Add richer WCF endpoint risk analysis beyond the current binding, security, credential, timeout, size, transfer mode, reader quota, and metadata exchange hints
- Improve severity classification as more discovery signals are added

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
- you need to identify configuration-heavy applications, connection strings, or custom configuration sections
- you want an initial list of modernisation review areas
- you need to identify likely migration risks before deeper analysis

---

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
