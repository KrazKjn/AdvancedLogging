# AdvancedLogging

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com/KrazKjn/AdvancedLogging)
[![NuGet Version](https://img.shields.io/badge/nuget-v1.0.0-blue.svg)](https://www.nuget.org/packages/AdvancedLogging/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.txt)

AdvancedLogging is a powerful and extensible logging framework for .NET applications. It is designed to provide developers with a rich set of tools to enhance their logging capabilities, with a focus on providing "DEBUGGING Session" level information to support staff.

## Why AdvancedLogging?

In today's complex application landscape, effective logging is not just a feature; it's a necessity. AdvancedLogging goes beyond simple log messages by providing a comprehensive set of features to help you diagnose and resolve issues quickly and efficiently.

-   **Automatic Method-Level Logging:** Automatically log method entry, exit, execution time, and parameters with a single line of code. This provides invaluable insight into the flow of your application and helps you pinpoint performance bottlenecks.
-   **Resilient Network Calls:** Automatically retry failed HTTP and SQL database calls with configurable retry policies. This helps make your application more resilient to transient network issues.
-   **Extensible and Decoupled:** Designed with dependency injection in mind, AdvancedLogging is decoupled from any specific logging library. It supports Log4Net and Serilog out of the box, and you can easily add support for any other logging framework by implementing a simple interface.
-   **Automated Code Rewriting:** The `AutoCoder` tool can automatically add logging and error handling to your C# and VB.NET code, saving you time and effort.

## Getting Started

### Installation

The easiest way to get started with AdvancedLogging is to install it from NuGet.

```powershell
Install-Package AdvancedLogging
```

### Configuration

AdvancedLogging is designed to be used with a dependency injection container. Here's how you can configure it in your application's startup code:

```csharp
using AdvancedLogging.Interfaces;
using AdvancedLogging.Loggers;
using AdvancedLogging.Logging.Interfaces;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Add the logger and its context to the service collection
        services.AddSingleton<ICommonLogger>(new Log4NetLogger("MyApplication"));
        services.AddSingleton<ILoggingContext, Log4NetLoggingContext>();

        // ... other services
    }
}
```

### Usage

Once you've configured the logger, you can inject it into your classes and start logging.

#### Basic Logging

```csharp
public class MyService
{
    private readonly ICommonLogger _logger;

    public MyService(ICommonLogger logger)
    {
        _logger = logger;
    }

    public void DoWork()
    {
        _logger.Info("Doing work...");
        _logger.Debug("This is a debug message.");
        _logger.Warn("This is a warning message.");
        _logger.Error("This is an error message.");
    }
}
```

#### Automatic Method Logging

To automatically log method entry, exit, and execution time, use the `AutoLogFunction`:

```csharp
public class MyService
{
    private readonly ICommonLogger _logger;
    private readonly ILoggingContext _loggingContext;

    public MyService(ICommonLogger logger, ILoggingContext loggingContext)
    {
        _logger = logger;
        _loggingContext = loggingContext;
    }

    public void DoWork(string myParameter)
    {
        using (new AutoLogFunction(_logger, _loggingContext, new { myParameter }))
        {
            // Your method logic here
        }
    }
}
```

This will produce log entries like this:

```
[Func: MyNamespace.MyService.DoWork (Method)]
(String)myParameter: "some value"
... your log messages ...
[End Func]: Time elapsed: 00:00:00.1234567
```

#### Auto Retry for HTTP Calls

To automatically retry failed HTTP calls, use the `GetAsync` extension method:

```csharp
public class MyService
{
    private readonly ICommonLogger _logger;
    private readonly ILoggingContext _loggingContext;
    private readonly HttpClient _httpClient;

    public MyService(ICommonLogger logger, ILoggingContext loggingContext, HttpClient httpClient)
    {
        _logger = logger;
        _loggingContext = loggingContext;
        _httpClient = httpClient;
    }

    public async Task<string> GetData()
    {
        // This will retry the request 3 times with a 1-second delay between retries
        var response = await _httpClient.GetAsync(_logger, _loggingContext, "http://example.com/api/data", 3, 1000);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
```

## AutoCoder

The `AutoCoder` is a Windows Forms application that can automatically add logging and error handling to your C# and VB.NET code. To use it, simply run the `AutoCoder.exe` and select the files you want to process.

## Contributing

Contributions are welcome! Please fork the repository and submit a pull request for any enhancements or bug fixes.

### Development

To build and run the project locally, you'll need:

-   Visual Studio 2022
-   .NET Framework 4.8

Open the `AdvancedLogging.sln` file in Visual Studio and build the solution.

## License

This project is licensed under the MIT License. See the [LICENSE.txt](LICENSE.txt) file for details.
