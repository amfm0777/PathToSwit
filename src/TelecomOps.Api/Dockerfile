# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/TelecomOps.Api/TelecomOps.Api.csproj", "src/TelecomOps.Api/"]
COPY ["src/TelecomOps.Core/TelecomOps.Core.csproj", "src/TelecomOps.Core/"]
COPY ["src/TelecomOps.Infrastructure/TelecomOps.Infrastructure.csproj", "src/TelecomOps.Infrastructure/"]
RUN dotnet restore "src/TelecomOps.Api/TelecomOps.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/TelecomOps.Api"
RUN dotnet build "TelecomOps.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "TelecomOps.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser:appuser /app
USER appuser

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TelecomOps.Api.dll"]