# Build from backend/src (NOT from inside the Api folder):
#   docker build -f EmployeeOnBoarding.Api/Dockerfile -t employee-onboarding-api .
# Or via compose from backend/:
#   docker compose up --build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore as a separate layer so dependency changes are cached.
COPY EmployeeOnBoarding.Contracts/EmployeeOnBoarding.Contracts.csproj EmployeeOnBoarding.Contracts/
COPY EmployeeOnBoarding.Api/EmployeeOnBoarding.Api.csproj EmployeeOnBoarding.Api/
RUN dotnet restore EmployeeOnBoarding.Api/EmployeeOnBoarding.Api.csproj

COPY EmployeeOnBoarding.Contracts/ EmployeeOnBoarding.Contracts/
COPY EmployeeOnBoarding.Api/ EmployeeOnBoarding.Api/

# Framework-dependent publish matches the aspnet runtime image below.
RUN dotnet publish EmployeeOnBoarding.Api/EmployeeOnBoarding.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EmployeeOnBoarding.Api.dll"]
