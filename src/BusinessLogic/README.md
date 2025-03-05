# BusinessLogic Project

## Overview
The BusinessLogic project is a .NET Framework 4.8 library that contains the core business logic for the application. It is responsible for implementing the business rules, calculations, and data processing required by the application.

## Project Structure
The project is organized into the following main components:

- **Services**: Contains service classes that encapsulate business logic and operations.
- **Models**: Contains data models and entities used throughout the business logic.
- **Repositories**: Contains repository classes for data access and manipulation.
- **Utilities**: Contains utility classes and helper functions used by the business logic.

## Getting Started
To get started with the BusinessLogic project, follow these steps:

1. **Clone the repository**: Clone the repository to your local machine using the following command:
			
	    git clone https://github.com/KrazKjn/AdvancedLogging.git

2. **Open the solution**: Open the solution file in Visual Studio.

3. **Build the project**: Build the project to restore the NuGet packages and compile the code.

## Usage
To use the BusinessLogic project in your application, follow these steps:

1. **Add a reference**: Add a reference to the BusinessLogic project in your application project.

2. **Instantiate services**: Instantiate the required service classes and use their methods to perform business operations.

1. **Example**:

        using AdvancedLogging.BusinessLogic;
        using AdvancedLogging.Extensions;
        using AdvancedLogging.Loggers;
        using AdvancedLogging.Logging;
        using AdvancedLogging.Models;
        using AdvancedLogging.SecureCredentials;
        using AdvancedLogging.Utilities;
 
        namespace MyApplication
        {
            class Program
            {
                static void Main(string[] args)
                {                    
                    string url = "https://learn.microsoft.com/en-us/aspnet/core/blazor/security/blazor-web-app-with-oidc?view=aspnetcore-9.0&pivots=without-bff-pattern";

                    ApplicationSettings.Logger = new Log4NetLogger("MyApplication");
                    SecurityProtocol.EnableAllTlsSupport();
 
                    WebClientExtended webClient = new WebClientExtended()
                    {
                        Credentials = System.Net.CredentialCache.DefaultCredentials,
                        Timeout = 10
                    };

                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: TestWebClient ...");
                        TestWebClient(webClient, new Uri(url));
                    }
                    catch { }
                }
            }

            private static void TestWebClient(WebClientExtended webClientExtended, Uri uri)
            {
                using (var vAutoLogFunction = new AutoLogFunction(new { webClientExtended }))
                {
                    try
                    {
                        webClientExtended.Timeout = 10;
                        string responseBody = webClientExtended.DownloadString(uri, ApplicationSettings.MaxAutoRetriesHttp, ApplicationSettings.AutoRetrySleepMsHttp, ApplicationSettings.AutoTimeoutIncrementMsHttp);

                        vAutoLogFunction.WriteDebug(4, new string('-', 80));
                        vAutoLogFunction.WriteDebugFormat(4, "Web Data: {0}", Utilities.ObjectDumper.Dump(responseBody));
                        vAutoLogFunction.WriteDebug(4, new string('-', 80));
                    }
                    catch (Exception exOuter)
                    {
                        vAutoLogFunction.LogFunction(new { webClientExtended }, MethodBase.GetCurrentMethod(), true, exOuter);
                        throw;
                    }
                }
            }
        }

## Contributing
Contributions to the BusinessLogic project are welcome. To contribute, follow these steps:

1. **Fork the repository**: Fork the repository on GitHub.

2. **Create a branch**: Create a new branch for your feature or bugfix.

3. **Make changes**: Make your changes and commit them with descriptive messages.

4. **Submit a pull request**: Submit a pull request to the main repository.

## License
This project is licensed under the MIT License. See the LICENSE file for more details.

## Contact
For any questions or issues, please contact the project maintainers.
