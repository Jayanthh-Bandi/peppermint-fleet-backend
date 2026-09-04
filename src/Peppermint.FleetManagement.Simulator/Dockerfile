# Stage 1: Build & Publish
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files and restore dependencies
COPY src/Peppermint.FleetManagement.Domain/Peppermint.FleetManagement.Domain.csproj src/Peppermint.FleetManagement.Domain/
COPY src/Peppermint.FleetManagement.Simulator/Peppermint.FleetManagement.Simulator.csproj src/Peppermint.FleetManagement.Simulator/
RUN dotnet restore src/Peppermint.FleetManagement.Simulator/Peppermint.FleetManagement.Simulator.csproj

# Copy full source and publish binaries
COPY src/ src/
RUN dotnet publish src/Peppermint.FleetManagement.Simulator/Peppermint.FleetManagement.Simulator.csproj -c Release -o /app/out

# Stage 2: Runtime Execution (Includes full .NET SDK in runtime to allow child process spawning via 'dotnet')
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "Peppermint.FleetManagement.Simulator.dll"]