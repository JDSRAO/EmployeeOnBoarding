
# Stage 1: Build the Azure Functions application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy project files
COPY EmployeeOnBoarding.Contracts/ EmployeeOnBoarding.Contracts/
COPY EmployeeOnBoarding.Functions/ EmployeeOnBoarding.Functions/

# Restore dependencies
RUN dotnet restore EmployeeOnBoarding.Functions/EmployeeOnBoarding.Functions.csproj

# Publish the application
RUN dotnet publish EmployeeOnBoarding.Functions/EmployeeOnBoarding.Functions.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

# Stage 2: Run using the official Azure Functions runtime
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0

WORKDIR /home/site/wwwroot

COPY --from=build /app/publish/ .

ENV AzureWebJobsScriptRoot=/home/site/wwwroot
ENV FUNCTIONS_WORKER_RUNTIME=dotnet-isolated
