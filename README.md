# CurrencyConverter Solution
This repository contains the API solution, a modular web application for retrieving exchange rates and converting currencies using .NET 9, ASP.NET Core, and MediatR.
The solution is orchestrated and run with [**.NET Aspire AppHost**](https://learn.microsoft.com/en-us/dotnet/aspire/).

# Future enhancements:
Because of lack of development time, not all the feature requests from the task were implemented. These are features that could be added in the future:
1) JWT authentication
2) Role-based access control (RBAC) for API endpoints.
3) Achieve 90%+ unit test coverage. 
4) Implement integration tests to verify API interactions. 
5) Provide test coverage reports. 
6) Implement API versioning for future-proofing.



## Prerequisites
Before you begin, make sure you have the following installed:
- **.NET 9 SDK**
  [Download .NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Aspire Workload**
  Install with:
``` shell
  dotnet workload install aspire
```
For more info see the [.NET Aspire documentation](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-install).
- **Git** (for cloning the repository)
- **Modern IDE**
  [JetBrains Rider](https://www.jetbrains.com/rider/) or [Visual Studio 2022+](https://visualstudio.microsoft.com/downloads/) or [Visual Studio Code](https://code.visualstudio.com/)

## Getting Started
### 1. Clone the Repository
``` shell
git clone https://github.com/80lvlpalladin/CurrencyConverter.git
cd CurrencyConverter
```
### 2. Restore Dependencies
``` shell
dotnet restore
```
### 3. Build the Solution
``` shell
dotnet build
```
### 4. Run the Solution using Aspire AppHost
The solution is orchestrated via **Aspire AppHost**. `CurrencyConverter.AppHost`
From the solution root, run:
``` shell
dotnet run --project [solutionFolder]/CurrencyConverter.AppHost
```
- The Aspire dashboard will open in your browser, showing all available services and endpoints, logs, metrics and traces.
- The **API endpoints** will start automatically as part of the AppHost startup.
