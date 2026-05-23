# LegacyLens.NET

LegacyLens.NET is a static discovery tool for unfamiliar, legacy, and modern .NET codebases.

It helps developers quickly understand the structure of a .NET solution by scanning project files and reporting useful information such as projects, target frameworks, project references, package references, and service-related configuration.

The aim is to help a developer who is new to a codebase answer questions such as:

- What projects exist in this solution?
- Which target frameworks are being used?
- Which projects depend on each other?
- Which NuGet packages are referenced?
- Are there signs of legacy technologies such as WCF?
- What diagrams or reports can help explain the system to others?

LegacyLens.NET is designed to work through static analysis, meaning it can provide useful information even when the target solution cannot currently be built.

---

## Current Status

LegacyLens.NET is currently in early MVP development.

The current implementation can scan a folder containing .NET projects and discover:

- `.csproj` files
- project names
- target frameworks
- project-to-project references
- NuGet package references

Example output:

```text
Projects discovered:
- SampleLegacyApp.Contracts
  Target framework: net48

- SampleLegacyApp.Data
  Target framework: net48
  Package reference: Dapper

- SampleLegacyApp.Services
  Target framework: net48
  Project reference: ..\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj
  Project reference: ..\SampleLegacyApp.Contracts\SampleLegacyApp.Contracts.csproj

- SampleLegacyApp.Web
  Target framework: net48
  Project reference: ..\SampleLegacyApp.Services\SampleLegacyApp.Services.csproj
  Project reference: ..\SampleLegacyApp.Contracts\SampleLegacyApp.Contracts.csproj
  Package reference: System.ServiceModel.Http
  Package reference: Newtonsoft.Json
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
- `.csproj`
- `packages.config`
- `app.config`
- `web.config`
- C# source files
- WCF configuration files
- project references
- package references

This makes it useful for old or broken solutions where restoring packages, installing SDKs, or compiling the code may not be possible immediately.

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

### Discovery

Responsible for finding projects, solutions, and source files.

Current discovery work includes:

- project discovery
- solution discovery
- source file discovery
- discovered project modelling

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

- WCF config scanning
- WCF endpoint modelling
- WCF service contract scanning

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

This scans the sample application and prints discovered project information to the console.

---

## Sample Output

```text
Projects discovered:
- SampleLegacyApp.Contracts
  Target framework: net48

- SampleLegacyApp.Data
  Target framework: net48
  Package reference: Dapper

- SampleLegacyApp.Services
  Target framework: net48
  Project reference: ..\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj
  Project reference: ..\SampleLegacyApp.Contracts\SampleLegacyApp.Contracts.csproj

- SampleLegacyApp.Web
  Target framework: net48
  Project reference: ..\SampleLegacyApp.Services\SampleLegacyApp.Services.csproj
  Project reference: ..\SampleLegacyApp.Contracts\SampleLegacyApp.Contracts.csproj
  Package reference: System.ServiceModel.Http
  Package reference: Newtonsoft.Json
```

---

## Planned MVP Features

The planned MVP is focused on producing useful discovery output for .NET solutions without requiring a successful build.

Planned features include:

- Markdown discovery report generation
- Mermaid dependency diagrams
- solution-level summary
- project dependency graph
- package reference summary
- target framework summary
- WCF endpoint and service contract detection
- basic risk indicators
- output files under the `output/` directory

---

## Planned Report Output

A future generated report may include sections such as:

```markdown
# LegacyLens.NET Discovery Report

## Projects

| Project | Target Framework | Project File |
|---|---|---|

## Project References

| From | To |
|---|---|

## Package References

| Project | Package |
|---|---|

## Mermaid Dependency Diagram
```

Example Mermaid diagram:

```mermaid
graph TD
    SampleLegacyApp.Web --> SampleLegacyApp.Services
    SampleLegacyApp.Services --> SampleLegacyApp.Data
    SampleLegacyApp.Services --> SampleLegacyApp.Contracts
    SampleLegacyApp.Data --> SampleLegacyApp.Contracts
```

---

## Development Roadmap

### Step 1: Static project discovery

Status: In progress

- Discover `.csproj` files
- Read project name
- Read target framework
- Read project references
- Read package references

### Step 2: Markdown report generation

Status: Planned

- Generate `output/discovery-report.md`
- Include project table
- Include project references
- Include package references

### Step 3: Dependency diagram generation

Status: Planned

- Generate Mermaid dependency graph
- Include graph in Markdown report

### Step 4: WCF discovery

Status: Planned / early implementation

- Detect WCF configuration
- Detect service contracts
- Detect endpoints

### Step 5: Risk and modernisation hints

Status: Planned

- Identify old target frameworks
- Identify legacy packages
- Highlight tightly coupled project dependencies
- Highlight WCF usage
- Highlight config-heavy applications

---

## Example Use Cases

LegacyLens.NET can be used when:

- you have inherited a legacy .NET application
- you need to understand a codebase before making changes
- the solution does not build locally
- you need to document project dependencies
- you want to create diagrams for stakeholders
- you are assessing modernisation effort
- you are preparing for refactoring or migration

---

## Design Principles

LegacyLens.NET is intended to be:

- static-first
- useful without requiring a successful build
- simple to run from the command line
- focused on practical codebase understanding
- useful for both legacy and modern .NET solutions
- able to generate human-readable reports and diagrams

---

## License

This project is licensed under the Apache License, Version 2.0, January 2004.

See the `LICENSE` file for details.