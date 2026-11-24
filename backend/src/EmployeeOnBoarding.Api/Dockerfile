# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /source

# Copy NuGet configuration and project files
COPY NuGet.config ./

COPY backend/src/EmployeeOnBoarding.Api/EmployeeOnBoarding.Api.csproj backend/src/EmployeeOnBoarding.Api/
COPY backend/src/EmployeeOnBoarding.Contracts/EmployeeOnBoarding.Contracts.csproj backend/src/EmployeeOnBoarding.Contracts/

# Restore dependencies
RUN dotnet restore backend/src/EmployeeOnBoarding.Api/EmployeeOnBoarding.Api.csproj

# Copy remaining source code
COPY backend/src/ backend/src/

# Build API
WORKDIR /source/backend/src/EmployeeOnBoarding.Api

RUN dotnet build EmployeeOnBoarding.Api.csproj -c Release --no-restore

# Stage 2: Publish
FROM build AS publish

RUN dotnet publish EmployeeOnBoarding.Api.csproj \
    -c Release \
    --no-build \
    -o /app

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=publish /app .

# Configure API listening port
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "EmployeeOnBoarding.Api.dll"]
