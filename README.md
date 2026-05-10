# TelecomOps Platform — Project Overview

A complete observability platform for simulated 5G network node management, built with .NET 8, Docker Compose, and a full observability stack.

## Architecture at a Glance

```
┌─────────────────────────────────────────────────────────────────┐
│                     External Clients                            │
│        (HTTP REST API @ localhost:5000)                         │
└────────────────────────┬────────────────────────────────────────┘
                         │
        ┌────────────────┴────────────────┐
        │                                 │
     ┌──▼──────────────┐      ┌──────────▼──┐
     │  TelecomOps.Api │      │ TelecomOps  │
     │                │      │  .Worker    │
     │ REST endpoints │      │             │
     │ Swagger docs   │      │ Telemetry   │
     └──┬─────────────┘      │ generator   │
        │                    │ every 5sec  │
        │                    └──┬──────────┘
        │                       │
        └───────────┬───────────┘
                    │
        ┌───────────▼────────────┐
        │  TelecomOps.Core       │
        │                        │
        │ Entities:              │
        │  - NodeConfig          │
        │  - TelemetryEvent      │
        │                        │
        │ Interfaces:            │
        │  - INodeConfigRepo     │
        │  - IMetricsPublisher   │
        │                        │
        │ No external deps       │
        └───────────┬────────────┘
                    │
        ┌───────────▼───────────────────┐
        │ TelecomOps.Infrastructure     │
        │                               │
        │ - AppDbContext (EF Core)      │
        │ - NodeConfigRepository        │
        │ - InfluxDbMetricsPublisher    │
        │ - PostgreSQL integration      │
        └───┬──────────────────┬────────┘
            │                  │
     ┌──────▼──┐        ┌──────▼──────┐
     │Postgres │        │  InfluxDB   │
     │          │        │             │
     │ nodeconf │        │ metrics     │
     │ configs  │        │ timeseries  │
     └──────────┘        └──────┬──────┘
                                │
                         ┌──────▼───────┐
                         │ Prometheus   │
                         │              │
                         │ Scrapes      │
                         │ /metrics     │
                         │ every 15sec  │
                         └──────┬───────┘
                                │
                         ┌──────▼────────┐
                         │  Grafana      │
                         │               │
                         │ Dashboards    │
                         │ Alerts        │
                         └───────────────┘
```

## Projects

### 1. TelecomOps.Core
**Domain layer** — No external dependencies.

- Entities: `NodeConfig`, `TelemetryEvent`
- Enums: `FrequencyBand`, `NodeStatus`
- Interfaces: `INodeConfigRepository`, `IMetricsPublisher`
- Services: `NodeConfigService`

**Read**: [TelecomOps.Core/README.md](src/TelecomOps.Core/README.md)

### 2. TelecomOps.Infrastructure
**Data access layer** — Implements Core interfaces.

- **AppDbContext**: EF Core with PostgreSQL
- **NodeConfigRepository**: CRUD operations
- **InfluxDbMetricsPublisher**: Metrics publishing
- NuGet: EF Core, Npgsql, InfluxDB.Client

**Read**: [TelecomOps.Infrastructure/README.md](src/TelecomOps.Infrastructure/README.md)

### 3. TelecomOps.Api
**HTTP REST API** — ASP.NET Core 8.

- Endpoints: GET/POST `/nodes`, GET `/nodes/active`
- Swagger documentation
- DI configuration
- Database schema initialization

**Read**: [TelecomOps.Api/README.md](src/TelecomOps.Api/README.md)

### 4. TelecomOps.Worker
**Background service** — Telemetry generation.

- Runs every 5 seconds (configurable)
- Generates fake metrics for active nodes
- Publishes to InfluxDB
- Uses `IServiceScopeFactory` to access Scoped repositories

**Read**: [TelecomOps.Worker/README.md](src/TelecomOps.Worker/README.md)

## Observability Stack

### PostgreSQL
- **Role**: Persistent node configuration storage
- **Port**: 5432
- **Database**: `telecomops`
- **Tables**: `nodeconfigs`

### InfluxDB
- **Role**: Time-series metric storage
- **Port**: 8086
- **Bucket**: `metrics`
- **Token**: `telecomops-token`

### Prometheus
- **Role**: Metrics scraping and aggregation
- **Port**: 9090
- **Scrape target**: `http://api:8080/metrics` every 15 seconds
- **Config**: [infra/prometheus/prometheus.yml](infra/prometheus/prometheus.yml)

**Read**: [infra/prometheus/PROMETHEUS-README.md](infra/prometheus/PROMETHEUS-README.md)

### Grafana
- **Role**: Visualization and dashboards
- **Port**: 3000
- **Default login**: admin / admin
- **Data sources**: Prometheus, InfluxDB (pre-configured)

## Dependency Rules

```
Core ← everything depends on this
  ↑
  ├── Infrastructure (implements Core)
  │     ↑
  │     ├── Api
  │     └── Worker
  
  ✅ Allowed: Api → Infrastructure → Core
  ❌ Forbidden: Core → Infrastructure (breaks Clean Architecture)
```

## Running the Project

### With Docker Compose

```bash
# Start (foreground, shows logs)
.\start.bat
# Choose option 1

# Start (background)
.\start.bat
# Choose option 2

# View logs
docker compose logs -f api
docker compose logs -f worker

# Stop and clean
.\start.bat
# Choose option 5
```

### Local development

```bash
# Terminal 1: Start PostgreSQL and dependencies
docker compose up postgres influxdb

# Terminal 2: Run API
dotnet run --project src/TelecomOps.Api

# Terminal 3: Run Worker
dotnet run --project src/TelecomOps.Worker
```

## URLs

| Service | URL | Credentials |
|---|---|---|
| **API** | http://localhost:5000 | — |
| **Swagger** | http://localhost:5000/swagger | — |
| **Prometheus** | http://localhost:9090 | — |
| **Grafana** | http://localhost:3000 | admin / admin |
| **InfluxDB** | http://localhost:8086 | admin / admin123456 |

## Configuration

All services configured via environment variables (see `docker-compose.yml`).

For local development, create `.env`:
```
ConnectionStrings__Default=Host=localhost;Port=5432;Database=telecomops;Username=telecom;Password=telecom123
InfluxDb__Url=http://localhost:8086
InfluxDb__Token=telecomops-token
InfluxDb__Org=telecomops
InfluxDb__Bucket=metrics
Worker__IntervalSeconds=5
```

## Common Tasks

### Create a new node
```bash
curl -X POST "http://localhost:5000/nodes?name=Node-Tokyo&frequencyBand=3500"
```

### View all nodes
```bash
curl "http://localhost:5000/nodes"
```

### View metrics in Prometheus
- Navigate to http://localhost:9090
- Query: `telecomops_node_latency_ms`

### View dashboards in Grafana
- Navigate to http://localhost:3000
- Dashboard: "TelecomOps Metrics" (if pre-configured)

## Documentation

- [DOCKER-COMPOSE-README.md](DOCKER-COMPOSE-README.md) — Detailed breakdown of docker-compose.yml
- [DOCKER-CHEATSHEET.md](DOCKER-CHEATSHEET.md) — Common Docker commands
- [K8S-CHEATSHEET.md](K8S-CHEATSHEET.md) — Kubernetes reference
- [CLAUDE.md](CLAUDE.md) — Original project brief and architecture notes

## Tests

Not yet implemented. Will add:
- Unit tests for Core (xUnit, FluentAssertions)
- Integration tests for Api (WebApplicationFactory)
- Mocks for Infrastructure

## Next Steps

1. ✅ Implement Clean Architecture structure
2. ✅ Docker Compose setup
3. ✅ Core domain layer
4. ✅ Infrastructure with EF Core and InfluxDB
5. ✅ REST API endpoints
6. ✅ Worker telemetry generation
7. ⏳ Unit and integration tests
8. ⏳ GitHub Actions CI/CD
9. ⏳ Kubernetes manifests (infra/k8s/)
10. ⏳ Advanced Grafana dashboards



Comandos:
docker compose exec postgres psql -U telecom -d telecomops : Abre psql prompt
comands psql:
        \dt : lista tablas