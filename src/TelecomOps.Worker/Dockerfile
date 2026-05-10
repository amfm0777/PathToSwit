# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/TelecomOps.Worker/TelecomOps.Worker.csproj", "src/TelecomOps.Worker/"]
COPY ["src/TelecomOps.Core/TelecomOps.Core.csproj", "src/TelecomOps.Core/"]
COPY ["src/TelecomOps.Infrastructure/TelecomOps.Infrastructure.csproj", "src/TelecomOps.Infrastructure/"]
RUN dotnet restore "src/TelecomOps.Worker/TelecomOps.Worker.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/TelecomOps.Worker"
RUN dotnet build "TelecomOps.Worker.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "TelecomOps.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser:appuser /app
USER appuser

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TelecomOps.Worker.dll"]