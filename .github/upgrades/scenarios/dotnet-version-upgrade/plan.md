# .NET 10.0 Upgrade Plan

## Overview

**Target**: Upgrade ClipYT solution from .NET 8.0 to .NET 10.0 (LTS)
**Scope**: 2 projects (~2.9k LOC), SDK-style Razor Pages application with test project

### Selected Strategy
**All-At-Once** — All projects upgraded simultaneously in a single operation.
**Rationale**: 2 projects, both on .NET 8.0, shallow dependency structure (ClipYT.Tests → ClipYT), straightforward upgrade with no complex migrations.

## Tasks

### 01-prerequisites: Verify SDK and toolchain

Ensure .NET 10.0 SDK is installed and compatible with the solution. Validate that global.json files (if present) allow .NET 10.0, and confirm toolchain readiness.

Assessment context: No global.json detected; standard SDK-style projects with no custom build tooling.

**Done when**: .NET 10.0 SDK verified installed; global.json validated or confirmed absent; solution ready for TFM updates

---

### 02-upgrade-projects: Upgrade all projects to .NET 10.0

Update target framework monikers from net8.0 to net10.0 across both projects. Update package references to .NET 10-compatible versions, addressing the incompatible package (Microsoft.VisualStudio.Azure.Containers.Tools.Targets) and the deprecated test package (xunit). Fix compilation errors from API changes.

Assessment context:
- **ClipYT.csproj** (main Razor Pages app): 58 API issues — 1 binary incompatible API (ConfigurationBinder.GetValue), 7 source incompatible (TimeSpan factory methods), 50 behavioral changes (Uri handling, HttpContent). 1 incompatible package.
- **ClipYT.Tests.csproj** (test project): 41 API issues — all behavioral changes (Uri-related). 1 deprecated package (xunit — update to latest).

Key concerns:
- **Binary incompatible**: ConfigurationBinder.GetValue signature changed — requires code adjustment
- **Source incompatible**: TimeSpan methods (FromSeconds, FromMinutes, FromMilliseconds) now generic — may need explicit type arguments
- **Behavioral changes**: Uri validation stricter in .NET 10; HttpContent.ReadAsStreamAsync behavior modified
- **Deprecated package**: xunit 2.9.0 marked deprecated — upgrade to latest stable version

**Done when**: Both projects target net10.0; all packages updated and restored; solution builds with 0 errors and 0 warnings; all tests pass

---

### 03-migrate-solution-format: Convert .sln to .slnx

Convert the solution file from legacy .sln format to the new XML-based .slnx format. This modernizes the solution structure and improves readability for source control.

User preference: Migrate to .slnx format as part of the .NET 10 upgrade.

**Done when**: ClipYT.slnx file created with all projects; solution opens correctly in Visual Studio; original .sln file removed

---

### 04-final-validation: Validate upgrade completion

Run full solution build, execute complete test suite, and verify application functionality. Document any behavioral changes or deferred recommendations.

**Done when**: Solution builds successfully; all tests pass; application runs without errors; upgrade summary documented
