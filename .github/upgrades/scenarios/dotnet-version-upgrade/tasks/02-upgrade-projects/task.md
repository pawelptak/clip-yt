# 02-upgrade-projects: Upgrade all projects to .NET 10.0

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

