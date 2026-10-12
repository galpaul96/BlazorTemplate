# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# This stage is used to build the web project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WebServices/ConnectX.Web/ConnectX.Web.csproj", "WebServices/ConnectX.Web/"]
COPY ["Common/ConnectX.Domain/ConnectX.Domain.csproj", "Common/ConnectX.Domain/"]
COPY ["Common/ConnectX.ServiceDefaults/ConnectX.ServiceDefaults.csproj", "Common/ConnectX.ServiceDefaults/"]
COPY ["WebServices/ConnectX.Web.Services/ConnectX.Web.Services.csproj", "WebServices/ConnectX.Web.Services/"]
COPY ["WebServices/ConnectX.Web.Gateway/ConnectX.Web.Gateway.csproj", "WebServices/ConnectX.Web.Gateway/"]
RUN dotnet restore "./WebServices/ConnectX.Web/ConnectX.Web.csproj"
COPY . .
WORKDIR "/src/WebServices/ConnectX.Web"
RUN dotnet build "./ConnectX.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the web project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ConnectX.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConnectX.Web.dll"]
