# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY EmployeeOnBoarding.Api/*.csproj EmployeeOnBoarding.Api/
COPY EmployeeOnBoarding.Contracts/*.csproj EmployeeOnBoarding.Contracts/
RUN dotnet restore EmployeeOnBoarding.Api/EmployeeOnBoarding.Api.csproj

# copy and build app and libraries
COPY EmployeeOnBoarding.Api/ EmployeeOnBoarding.Api/
COPY EmployeeOnBoarding.Contracts/ EmployeeOnBoarding.Contracts/
WORKDIR /source/EmployeeOnBoarding.Api
RUN dotnet build -c Release --self-contained

## test stage -- exposes optional entrypoint
## target entrypoint with: docker build --target test
#FROM build AS test
#WORKDIR /source/tests
#
#COPY tests/*.csproj .
#RUN dotnet restore tests.csproj

#COPY tests/ .
#RUN dotnet build --no-restore

ENTRYPOINT ["dotnet", "test", "--logger:trx", "--no-restore", "--no-build"]

FROM build AS publish
RUN dotnet publish -c Release -r linux-x64 --self-contained true -p:GenerateRuntimeConfigurationFiles=false -p:GenerateDependencyFile=false --no-build -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "EmployeeOnBoarding.Api.dll"]