# Blazor

A .NET 10 application built as a Blazor Web App with Interactive Server rendering. The solution separates the web frontend and its gateway from the API and shared domain/services projects.

## Solution layout

- `src/Blazor/Blazor.AppHost` orchestrates the local distributed application with .NET Aspire.
- `src/Blazor/WebServices/Blazon.Web` hosts the Blazor UI and ASP.NET Core Identity.
- `src/Blazor/WebServices/Blazor.Web.Gateway` provides the web-to-API client.
- `src/Blazor/ApiServices/Blazor.Api` hosts the HTTP API; `Blazor.Api.Ef` contains persistence support.
- `src/Blazor/Common` contains shared domain, service, and Aspire service-default projects.
- `src/Blazor/Services/Blazor.MongoDb` contains MongoDB integration.
- `src/Blazor/Blazor.Tests` contains the test project.

## Run locally

Requirements: .NET 10 SDK and Docker for the Aspire-managed PostgreSQL, MongoDB, and Redis dependencies.

The AppHost development settings include a test-only shared secret for local runs. Set `ApiAuthentication__SharedSecret` in the environment that launches AppHost to override it, then run:

```powershell
dotnet run --project src/Blazor/Blazor.AppHost/Blazor.AppHost.csproj
```

AppHost passes the secret to the API and web host for Gateway-to-API authentication. Keep deployment secrets in the deployment environment or secret store, not in source control.

## Documentation

Project documentation is indexed in [`docs/README.md`](docs/README.md). The existing [compliance checklist](src/Blazor/COMPLIANCE_CHECKLIST.md) records source-level review notes.
