
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /source

# Copy project files
COPY EmployeeOnBoarding.Api/*.csproj EmployeeOnBoarding.Api/
COPY EmployeeOnBoarding.Contracts/*.csproj EmployeeOnBoarding.Contracts/

# Restore dependencies
RUN dotnet restore EmployeeOnBoarding.Api/EmployeeOnBoarding.Api.csproj

# Copy source code
COPY EmployeeOnBoarding.Api/ EmployeeOnBoarding.Api/
COPY EmployeeOnBoarding.Contracts/ EmployeeOnBoarding.Contracts/

# Publish application
WORKDIR /source/EmployeeOnBoarding.Api
RUN dotnet publish -c Release --no-self-contained -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "EmployeeOnBoarding.Api.dll"]
