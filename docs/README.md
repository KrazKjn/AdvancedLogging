# AdvancedLogging Solution

## Overview

The AdvancedLogging solution is a comprehensive logging framework designed to enhance the logging capabilities of C# and VB.NET applications. It includes various projects targeting different aspects of logging, such as advanced logging mechanisms, Auto Coder for automated code rewriting adding logging and error handling, and unit tests to ensure the reliability of the logging framework. The solution focuses on unhandled exceptions and provides a robust logging mechanism to capture and handle unhandled errors effectively and verbosely. In essence, the AdvancedLogging solution aims to provide DEBUGGING Session level information to the support staff. The solution support Log4Net and SeriLog and any other logging framework. Other noteworthy features include the ability trace thread instances and indented logging showing call hierarchy. Included is a large set of Extension classes providing Web and Data "Retry on Timeout" methods for many of the action Methods. The extension functions support Max Retries, Retry Delay, Incremented Timeout Value, and logging.

        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>

The ApplicationSettings library is able to monitor the Application Config file and reload changes at run-time.

Examples will be created as time permits. Until then, use the AdvancedLogging.TestConsoleApp project as a beginning point.

## Projects

### 1. Logging
This project contains the core logging functionality, including various loggers and utilities to facilitate advanced logging in applications.

### 2. AutoCoder
AutoCoder is a project designed to automatically add AutoLogging and/or Unhandled Exception error handling to C# and VB.NET code. It leverages the Roslyn API to analyze and modify the code, ensuring that selected methods, properties, constructors, functions, etc., have the requested enhancements.

### 3. UnitTests
This project contains unit tests for the AdvancedLogging project. It ensures that the logging functionality works as expected and helps maintain the reliability of the logging framework.

## Features

- Advanced logging mechanisms for C# and VB.NET applications.
- Automatic code rewriting to add logging and error handling using the Roslyn API.
- Comprehensive unit tests to ensure the reliability of the logging framework.

## Prerequisites

- .NET Framework 4.8
- C# 7.3
- Visual Studio (or any compatible IDE)

## Installation

1. Clone the repository:

	   git clone https://github.com/KrazKjn/AdvancedLogging.git
	   
2. Open the solution in Visual Studio.

## Usage

### AdvancedLogging

1. **Initialize the Logger:**
    Create an instance of the desired logger (e.g., `Log4NetLogger`) and configure it as needed.
    
		var logger = new Log4NetLogger("MyLogger");

2. **Log Messages:**
    Use the logger to log messages at different levels (e.g., Debug, Info, Error).			

		logger.Debug("This is a debug message.");
		logger.Debug(4, "This is a debug message.");
		logger.Info("This is an info message.");
		logger.Error("This is an error message.");


## Contributing

Contributions are welcome! Please fork the repository and submit pull requests for any enhancements or bug fixes.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Contact

For any questions or issues, please open an issue on the GitHub repository or contact the maintainer.

---

This ReadMe file provides an overview of the AdvancedLogging solution, its purpose, and instructions on how to use it. It also includes information on prerequisites, installation, usage examples, contributing, and licensing.
