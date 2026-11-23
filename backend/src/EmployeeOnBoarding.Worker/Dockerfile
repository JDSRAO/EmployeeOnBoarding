FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY EmployeeOnBoarding.Worker/*.csproj EmployeeOnBoarding.Worker/
COPY EmployeeOnBoarding.Contracts/*.csproj EmployeeOnBoarding.Contracts/
RUN dotnet restore EmployeeOnBoarding.Worker/EmployeeOnBoarding.Worker.csproj

COPY EmployeeOnBoarding.Worker/ EmployeeOnBoarding.Worker/
COPY EmployeeOnBoarding.Contracts/ EmployeeOnBoarding.Contracts/
WORKDIR /source/EmployeeOnBoarding.Worker
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EmployeeOnBoarding.Worker.dll"]