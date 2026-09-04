# Stage 1: Build & Publish
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files and restore dependencies
COPY src/Peppermint.FleetManagement.Domain/Peppermint.FleetManagement.Domain.csproj src/Peppermint.FleetManagement.Domain/
COPY src/Peppermint.FleetManagement.Application/Peppermint.FleetManagement.Application.csproj src/Peppermint.FleetManagement.Application/
COPY src/Peppermint.FleetManagement.Infrastructure/Peppermint.FleetManagement.Infrastructure.csproj src/Peppermint.FleetManagement.Infrastructure/
COPY src/Peppermint.FleetManagement.Api/Peppermint.FleetManagement.Api.csproj src/Peppermint.FleetManagement.Api/
RUN dotnet restore src/Peppermint.FleetManagement.Api/Peppermint.FleetManagement.Api.csproj

# Copy full source and publish binaries
COPY src/ src/
RUN dotnet publish src/Peppermint.FleetManagement.Api/Peppermint.FleetManagement.Api.csproj -c Release -o /app/out

# Stage 2: Runtime Execution
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "Peppermint.FleetManagement.Api.dll"]