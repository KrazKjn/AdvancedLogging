# Migration Roadmap: Upgrading to .NET Standard 2.0 & .NET 8.0

## Executive Summary

This document provides a comprehensive, step-by-step roadmap for upgrading the **Enterprise Observability and Resiliency Framework** (AdvancedLogging solution) from .NET Framework 4.8 to **.NET Standard 2.0** and **.NET 8.0**.

The roadmap is ordered from the **least intrusive / easiest** projects to the **most complex / application** projects, and establishes standard patterns for isolating Windows-specific components into OS-targeted classes.

---

## 1. Architectural Strategy & OS Component Handling

### Target Framework Selection Strategy
- **Shared Class Libraries**: Dual-target `<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>` to maintain compatibility with both legacy .NET Framework consumers and modern .NET 8 environments.
- **Executable Apps**: Target `<TargetFramework>net8.0</TargetFramework>` for cross-platform console/test applications, and `<TargetFramework>net8.0-windows</TargetFramework>` for Windows desktop UI applications (AutoCoder).
- **OS Guarding**: Utilize `OperatingSystem.IsWindows()` at runtime or `#if WINDOWS` / `#if NETFRAMEWORK` preprocessor directives to safely guard Windows-only APIs.

### Windows Component Replacement Matrix

| Windows Component | Current Usage | Modern Replacement Strategy | Target NuGet Package / API |
| :--- | :--- | :--- | :--- |
| `System.Configuration` | App settings reading & dynamic update in `Logging`, `DataAccess`, `BusinessLogic`. | Replace with `System.Configuration.ConfigurationManager` for app.config compatibility; support `Microsoft.Extensions.Configuration` for .NET 8 apps. | `System.Configuration.ConfigurationManager` |
| `System.Data.SqlClient` | SQL parameter/command extension logging and data access. | Replace all usages with `Microsoft.Data.SqlClient` across all projects. | `Microsoft.Data.SqlClient` |
| `System.ServiceProcess` | Windows Service control and `WinServiceBase`. | Use `System.ServiceProcess.ServiceController` guarded by `OperatingSystem.IsWindows()`. Move service hosting in .NET 8 to `Microsoft.Extensions.Hosting.WindowsServices`. | `System.ServiceProcess.ServiceController` |
| `System.Web` | Legacy `HttpApplicationState` extensions & `WebServiceBase` (`HttpApplication`). | Wrap legacy `System.Web` APIs with `#if NETFRAMEWORK`. Introduce `Microsoft.AspNetCore.Http` / middleware wrappers for .NET 8. | `Microsoft.AspNetCore.Http.Abstractions` |
| `Microsoft.Win32` & `System.Security.AccessControl` | Registry key access in `CurrentRegistryKey.cs`. | Replace with `Microsoft.Win32.Registry` NuGet package; abstract platform calls behind `IRegistryKey`. | `Microsoft.Win32.Registry` |
| `System.Security` | Cryptography (`SHA256CryptoServiceProvider`, `TripleDESCryptoServiceProvider`). | Use standard cross-platform `System.Security.Cryptography` types (`SHA256.Create()`, `Aes.Create()`). | Built-in .NET Standard 2.0 / .NET 8 |
| `System.Management` | Windows Management Instrumentation (WMI) access. | Use `System.Management` package with `OperatingSystem.IsWindows()` runtime guards. | `System.Management` |
| `System.Diagnostics.EventLog` | Windows event logging. | Use `System.Diagnostics.EventLog` package with `OperatingSystem.IsWindows()` runtime guards or Serilog EventLog sink. | `System.Diagnostics.EventLog` |
| `System.Diagnostics.PerformanceCounter` | System performance counters. | Use `System.Diagnostics.PerformanceCounter` package with `OperatingSystem.IsWindows()` guards or System.Diagnostics.Metrics / OpenTelemetry meters. | `System.Diagnostics.PerformanceCounter` |

---

## 2. Project-by-Project Migration Order

### Step 1: `AdvancedLogging.BusinessEntities` (Easiest / Foundation)
- **Target Frameworks**: `<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>`
- **Complexity**: Low (No external dependencies)
- **Action Items**:
  1. Convert `AdvancedLogging.BusinessEntities.csproj` to SDK-style format.
  2. Verify all models (`Application`, `Client`, `Configuration`, `SystemStatus`) compile cleanly for both targets.

### Step 2: `AdvancedLogging.Utilities` (Low Complexity)
- **Target Frameworks**: `<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>`
- **Dependencies**: `BusinessEntities`
- **Action Items**:
  1. Convert `AdvancedLogging.Utilities.csproj` to SDK-style format.
  2. Isolate Win32 Credential UI P/Invoke calls (`CredentialsDialog.cs`, `CREDUI.cs`, `GDI32.cs`):
     - Wrap P/Invoke methods with `[SupportedOSPlatform("windows")]` or `OperatingSystem.IsWindows()` checks.
     - Move Windows-only UI code into a Windows-targeted class file (`CredentialsDialog.Windows.cs`).
  3. Ensure core security utilities (`SecurityProtocol.cs`, `Utils.cs`) compile cleanly for cross-platform targets.

### Step 3: `AdvancedLogging.Logging` (Moderate Complexity)
- **Target Frameworks**: `<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>`
- **Dependencies**: `BusinessEntities`, `Utilities`
- **Action Items**:
  1. Convert `AdvancedLogging.Logging.csproj` to SDK-style format.
  2. Replace `System.Data.SqlClient` references with `Microsoft.Data.SqlClient` in `LoggingExtensions.cs` and `LoggingUtils.cs`.
  3. Reference `System.Configuration.ConfigurationManager` for configuration handling in `LoggingUtils.cs`.
  4. Update NuGet packages (`log4net`, `Serilog`) to modern .NET Standard 2.0 / .NET 8 compatible versions.

### Step 4: `AdvancedLogging.DataAccess` (Moderate-High Complexity)
- **Target Frameworks**: `<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>`
- **Dependencies**: `BusinessEntities`, `Utilities`, `Logging`
- **Action Items**:
  1. Convert `AdvancedLogging.DataAccess.csproj` to SDK-style format.
  2. Migrate all references from `System.Data.SqlClient` to `Microsoft.Data.SqlClient` in `SqlHelper.cs`, `SqlDataReaderHelper.cs`, `VersionInfo.cs`, and `DataExtensions.cs`.
  3. Address `System.Web` in `HttpApplicationStateExtensions.cs`:
     - Conditionally guard legacy `System.Web.HttpApplicationState` with `#if NETFRAMEWORK`.
     - Implement `IDistributedCache` / `IMemoryCache` extensions for .NET 8.
  4. Refactor `WebClientExtended.cs`:
     - Provide `HttpClient`-backed implementation for .NET Standard 2.0 and .NET 8.

### Step 5: `AdvancedLogging.BusinessLogic` (High Complexity)
- **Target Frameworks**: `<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>`
- **Dependencies**: `BusinessEntities`, `Utilities`, `Logging`, `DataAccess`
- **Action Items**:
  1. Convert `AdvancedLogging.BusinessLogic.csproj` to SDK-style format.
  2. Reference NuGet packages: `Microsoft.Win32.Registry`, `System.ServiceProcess.ServiceController`, `System.Configuration.ConfigurationManager`, `Microsoft.Data.SqlClient`.
  3. Refactor `CurrentRegistryKey.cs` to use `Microsoft.Win32.Registry` with `OperatingSystem.IsWindows()` runtime guards.
  4. Refactor `CurrentServiceController.cs` and `WinServiceBase.cs` to use `System.ServiceProcess.ServiceController` with OS checks or Windows Background Service host classes.
  5. Refactor `WebServiceBase.cs`:
     - Guard `System.Web.HttpApplication` with `#if NETFRAMEWORK`.
     - Add ASP.NET Core middleware base class for .NET 8.

### Step 6: `AdvancedLogging.TestExtensions` (Moderate Complexity)
- **Target Frameworks**: `<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>`
- **Dependencies**: Framework libraries
- **Action Items**:
  1. Convert `AdvancedLogging.TestExtensions.csproj` to SDK-style format.
  2. Update registry and service controller test helpers (`CurrentRegistryKeyTest`, `CurrentServiceControllerTest`) to implement updated abstractions.

### Step 7: `AdvancedLogging.UnitTests` (Test Suite)
- **Target Frameworks**: `<TargetFrameworks>net48;net8.0</TargetFrameworks>` (or `<TargetFramework>net8.0</TargetFramework>`)
- **Action Items**:
  1. Update project file to SDK-style targeting .NET 8.0.
  2. Upgrade NUnit and test runner packages.
  3. Validate all unit tests pass on .NET 8.

### Step 8: `AdvancedLogging.TestConsoleApp` (Console Sample App)
- **Target Framework**: `<TargetFramework>net8.0</TargetFramework>`
- **Action Items**:
  1. Convert project file to SDK-style.
  2. Verify app settings and logging execute correctly on .NET 8.

### Step 9: `AdvancedLogging.AutoCoder` (WinForms App)
- **Target Framework**: `<TargetFramework>net8.0-windows</TargetFramework>` with `<UseWindowsForms>true</UseWindowsForms>`
- **Action Items**:
  1. Convert project file to SDK-style targeting `net8.0-windows`.
  2. Upgrade Roslyn packages (`Microsoft.CodeAnalysis.CSharp`, `Microsoft.CodeAnalysis.VisualBasic`) to modern versions.

---

## 3. Verification & Validation Checklist

- [ ] All class libraries compile cleanly for both `netstandard2.0` and `net8.0`.
- [ ] No direct dependencies on `System.Data.SqlClient` remain; `Microsoft.Data.SqlClient` is used throughout.
- [ ] Windows P/Invoke calls and OS-specific services are properly guarded with `OperatingSystem.IsWindows()`.
- [ ] Legacy `System.Web` references are isolated or guarded with `#if NETFRAMEWORK`.
- [ ] Automated unit test suite executes and passes on .NET 8.
