# Enterprise Observability and Resiliency Framework

### *(Originally developed as AdvancedLogging / AutoLogging)*

## Overview

The **Enterprise Observability and Resiliency Framework** is an engineering framework designed to make enterprise software systems observable, diagnosable, and resilient by design.

The project began as **AdvancedLogging**, a logging library intended to improve diagnostic information captured during application failures. As the framework matured through practical use on enterprise software projects, it became clear that the underlying concepts extended far beyond logging.

The project is now being **re-architected and rebranded** as the **Enterprise Observability and Resiliency Framework** to better reflect its broader objective:

> **Successful systems should explain themselves.**

Rather than viewing logging as an isolated concern, the framework treats observability as a foundational engineering capability that includes:

* Structured logging
* Automatic execution tracing
* Exception diagnostics
* Operational telemetry
* Performance measurement
* Retry management
* Health monitoring
* Execution context capture
* Production diagnostics
* Engineering evidence for root-cause analysis

The long-term objective is to reduce Mean Time to Detect (MTTD), Mean Time to Root Cause (MTTRC), and Mean Time to Recover (MTTR) by enabling engineers to diagnose production issues directly from operational telemetry instead of reproducing failures.

---

# Project Status

The current implementation successfully demonstrates the engineering concepts described throughout the accompanying **Engineering Project Excellence** article series.

However, the existing architecture was developed over many years as an internal engineering platform and now requires modernization before it would be appropriate for high-volume enterprise or cloud-native production environments.

Future versions will focus on:

* Reduced runtime overhead
* Improved scalability
* Distributed tracing
* Cloud-native architecture
* OpenTelemetry integration
* Modern dependency injection
* Metrics collection
* Health endpoints
* Operational dashboards
* Expanded resiliency capabilities

This repository should therefore be viewed as both:

* a functional implementation of proven engineering concepts, and
* the foundation for the next-generation Enterprise Observability and Resiliency Framework.

---

# Engineering Philosophy

Most logging frameworks answer the question:

> **"What happened?"**

The Enterprise Observability and Resiliency Framework attempts to answer much more important engineering questions:

* What code executed?
* Which execution path was followed?
* What parameter values entered the operation?
* What retries occurred?
* How long did each operation require?
* Where did execution fail?
* What happened immediately before the failure?
* Why did the application behave the way it did?

The objective is not to generate larger log files.

The objective is to generate actionable engineering evidence.

Every production incident should provide sufficient information to begin root-cause analysis immediately without requiring engineers to reproduce the problem whenever possible.

---

# Core Capabilities

The framework currently provides:

* Automatic execution tracing
* Structured logging
* Thread-aware execution tracking
* Call hierarchy visualization
* Automatic parameter collection
* Exception logging
* Retry-aware diagnostics
* Performance timing
* Configurable logging providers
* Runtime configuration reload
* Roslyn-powered automatic instrumentation
* Timeout retry extensions for Web and Data operations

Supported logging providers currently include:

* Log4Net
* Serilog

The architecture has been designed to support additional providers with minimal effort.

---

# Solution Projects

## Logging

Contains the core framework responsible for execution tracing, structured diagnostics, retry support, thread tracking, configuration management, and operational logging.

The framework emphasizes production diagnostics rather than traditional application logging.

---

## AutoCoder

AutoCoder uses the Roslyn compiler platform to automatically instrument existing C# and VB.NET applications.

Instead of requiring developers to manually add logging throughout an application, AutoCoder can automatically insert:

* Execution tracing
* Exception handling
* Parameter capture
* Performance measurement
* Logging instrumentation

into selected:

* Methods
* Constructors
* Properties
* Functions
* Events

This allows large existing codebases to adopt observability with minimal manual effort.

---

## Unit Tests

Contains automated tests validating framework functionality and ensuring reliability as the framework evolves.

---

# Retry Extensions

The framework contains numerous extension methods supporting resilient data and web operations.

These extensions provide configurable:

* Maximum retry count
* Retry delay
* Automatic timeout increment
* Structured retry logging

```csharp
/// <param name="retries">
/// Number of retry attempts.
/// </param>

/// <param name="retryWaitMS">
/// Delay between retries.
/// </param>

/// <param name="autoTimeoutIncrement">
/// Timeout increase after timeout exceptions.
/// </param>
```

These capabilities reduce repetitive boilerplate while providing consistent operational diagnostics.

---

# Runtime Configuration

The ApplicationSettings library monitors configuration changes and automatically reloads settings without requiring application restarts.

This allows production diagnostics to be adjusted dynamically with minimal operational impact.

---

# Why This Framework Exists

One of the most expensive activities in enterprise software maintenance is reproducing production failures.

Traditional debugging often follows this cycle:

1. Production issue reported
2. Attempt to reproduce
3. Add additional logging
4. Redeploy
5. Wait for failure
6. Gather more information
7. Repeat

Every deployment introduces engineering cost, operational risk, and customer disruption.

The Enterprise Observability and Resiliency Framework was designed to reduce or eliminate this cycle by capturing sufficient diagnostic context during the original failure.

Rather than asking:

> *"Can we reproduce it?"*

Engineering can begin asking:

> *"Why did it happen?"*

---

# Current Limitations

Although the current implementation demonstrates the framework's concepts, several architectural improvements have been identified through years of practical use.

Areas planned for redesign include:

* Performance optimization
* Reduced reflection overhead
* Improved instrumentation architecture
* Modern dependency injection
* Cloud-native deployment
* Distributed tracing
* Metrics aggregation
* Health monitoring
* OpenTelemetry compatibility
* Dashboard integration

These improvements will form the basis of the next generation of the Enterprise Observability and Resiliency Framework.

---

# Getting Started

Examples will be expanded over time.

Until then, the **AdvancedLogging.TestConsoleApp** project demonstrates the current implementation and serves as the primary reference application.

---

# Installation

```text
git clone https://github.com/KrazKjn/AdvancedLogging.git
```

Open the solution in Visual Studio.

Requirements:

* .NET Framework 4.8
* C# 7.3
* Visual Studio

---

# Contributing

Contributions, suggestions, and engineering discussions are welcome.

The framework is evolving through practical experience and continuous refinement.

---

# License

MIT License

---

# Related Publications

This framework accompanies the **Engineering Project Excellence** article series on LinkedIn.

The series discusses the engineering principles that influenced the design of this framework, including:

* Planning
* Architecture
* Technical Debt
* Testing
* Security
* Performance
* Engineering Memory
* Observability
* Long-term Maintainability
