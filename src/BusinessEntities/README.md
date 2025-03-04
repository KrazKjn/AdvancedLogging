# BusinessEntities

## Overview

The BusinessEntities project is a part of the AdvancedLogging solution. It contains the core business logic and services that interact with the logging framework. This project is designed to handle web service requests, manage security protocols, and log SOAP messages for debugging and monitoring purposes.

## Purpose

The primary purpose of the BusinessEntities project is to provide a robust foundation for handling web service requests and ensuring that all interactions are properly logged. This helps in maintaining the reliability and traceability of the application.

## Features

- Handles web service requests and logs SOAP messages.
- Manages security protocols to ensure secure communication.
- Provides automatic logging of function calls and error handling using the `AutoLogFunction` utility.

## Prerequisites

- .NET Framework 4.8
- C# 7.3
- Visual Studio (or any compatible IDE)

## Installation

1. Clone the repository:
     
        git clone https://github.com/KrazKjn/AdvancedLogging.git

2. Open the solution in Visual Studio.

## Usage

### WebServiceBase

1. **Initialize the Web Service:**
    The `WebServiceBase` class is designed to be inherited by web service classes. It initializes security protocols and sets up logging for SOAP messages.
    
        public class MyWebService : WebServiceBase
        {
            // Your web service methods here
        }

2. **Log SOAP Messages:**
    The `Application_BeginRequest` method logs the SOAP action and optionally the SOAP body if `LogSoapBody` is set to `true`.
    
        public void Application_BeginRequest(Object Sender, EventArgs e)
        {
            // Logging logic is handled automatically
        }

3. **Example:**
    Below is an example of how to use the `WebServiceBase` class in a web service.
    
        using System.Web.Services;

        namespace MyNamespace
        {
            [WebService(Namespace = "http://tempuri.org/")]
            public class MyWebService : WebServiceBase
            {
                [WebMethod]
                public string HelloWorld()
                {
                    return "Hello, World!";
                }
            }
        }

## Contributing

Contributions are welcome! Please fork the repository and submit pull requests for any enhancements or bug fixes.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Contact

For any questions or issues, please open an issue on the GitHub repository or contact the maintainer.

---

This ReadMe file provides an overview of the BusinessEntities project, its purpose, and instructions on how to use it. It also includes information on prerequisites, installation, usage examples, contributing, and licensing.
