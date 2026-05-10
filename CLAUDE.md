# TelecomOps Platform — Contexto del proyecto

## Quién soy

Senior Software Engineer con 20+ años de experiencia. Stack principal: C#/.NET, WPF, WCF, C++, arquitecturas multi-hilo. Trabajo en Keysight Technologies sobre la plataforma UXM 5G (E7515B) — configuración de stacks de protocolo 3GPP 4G/5G. Tengo experiencia real con Grafana/InfluxDB/Loki/Prometheus en entornos de test 5G.

**Objetivo:** Construir un portfolio público en GitHub para conseguir un puesto Senior/Lead en Suiza (Zürich). Este proyecto debe demostrar arquitectura .NET moderna a recruiters de empresas como Ericsson CH, Nokia, Zühlke Engineering, u-blox.

---

## Qué es este proyecto

**TelecomOps Platform** es una plataforma de gestión y monitorización de nodos de red simulados, construida con .NET 8, que combina:

- API REST para configurar parámetros de nodos 5G (dominio ficticio, sin código propietario)
- Background Worker que genera métricas de telemetría (latencia, throughput, estado)
- Stack de observabilidad completo: Grafana + Prometheus + InfluxDB
- Persistencia con PostgreSQL + EF Core
- Todo orquestado con Docker Compose

---

## Arquitectura — Clean Architecture en 4 capas

```
TelecomOps/
├── src/
│   ├── TelecomOps.Api/            ← ASP.NET Core 8 REST API
│   ├── TelecomOps.Worker/         ← Background Worker Service
│   ├── TelecomOps.Core/           ← Domain + interfaces (sin dependencias externas)
│   └── TelecomOps.Infrastructure/ ← EF Core, InfluxDB, repositorios
├── tests/
│   ├── TelecomOps.Api.Tests/
│   ├── TelecomOps.Worker.Tests/
│   └── TelecomOps.Core.Tests/
├── infra/
│   ├── grafana/
│   │   ├── dashboards/
│   │   └── provisioning/
│   ├── prometheus/
│   └── k8s/
├── .github/workflows/ci.yml
├── docker-compose.yml
├── docker-compose.override.yml
└── TelecomOps.sln
```

**Regla de dependencias:**
- `Core` → sin dependencias NuGet externas. Solo tipos primitivos y abstracciones.
- `Infrastructure` → implementa interfaces de `Core`. Aquí viven EF Core, InfluxDB client, etc.
- `Api` y `Worker` → referencian `Core` e `Infrastructure`. Nunca al revés.

---

## Stack tecnológico

| Capa | Tecnología |
|---|---|
| API | ASP.NET Core 8, minimal API style con controllers |
| Worker | .NET 8 BackgroundService / IHostedService |
| ORM | Entity Framework Core 8 + PostgreSQL |
| Métricas | InfluxDB 2 (Flux) + Prometheus (via `/metrics` endpoint) |
| Dashboards | Grafana 10 |
| Mensajería | (Proyecto 2) RabbitMQ + MassTransit |
| Tests | xUnit, FluentAssertions, WebApplicationFactory |
| CI/CD | GitHub Actions |
| Contenedores | Docker Compose (dev), Kubernetes manifests (infra/k8s/) |

---

## Ficheros ya creados

### Infraestructura Docker (listos, no modificar salvo que se pida)

- `docker-compose.yml` — orquesta API, Worker, PostgreSQL, InfluxDB, Prometheus, Grafana
- `docker-compose.override.yml` — overrides para desarrollo local
- `infra/prometheus/prometheus.yml` — scrape config, apunta a `api:8080/metrics`
- `infra/grafana/provisioning/datasources/datasources.yml` — Prometheus + InfluxDB como datasources
- `infra/grafana/provisioning/dashboards/dashboards.yml` — provider de dashboards
- `src/TelecomOps.Api/Dockerfile` — multi-stage build, non-root user
- `src/TelecomOps.Worker/Dockerfile` — multi-stage build, non-root user
- `.gitignore`

### Variables de entorno relevantes (definidas en docker-compose.yml)

```
ConnectionStrings__Default=Host=postgres;Port=5432;Database=telecomops;Username=telecom;Password=telecom123
InfluxDb__Url=http://influxdb:8086
InfluxDb__Token=telecomops-token
InfluxDb__Org=telecomops
InfluxDb__Bucket=metrics
Worker__IntervalSeconds=5
```

---

## Lo que falta por construir (orden de prioridad)

### 1. TelecomOps.Core (empezar aquí)

```csharp
// Entidades
NodeConfig.cs          // Id, Name, FrequencyBand, TransmitPower, Status, CreatedAt, UpdatedAt
TelemetryEvent.cs      // NodeId, Timestamp, Latency, Throughput, SignalStrength

// Enums
FrequencyBand.cs       // N78, N257, N258, N260 (bandas 5G reales)
NodeStatus.cs          // Active, Inactive, Maintenance, Error

// Interfaces
INodeConfigRepository.cs   // GetAll, GetById, Add, Update, Delete
IMetricsPublisher.cs       // Publish(TelemetryEvent)

// Servicio de dominio
NodeConfigService.cs   // Lógica de negocio: validaciones, reglas
```

### 2. TelecomOps.Infrastructure

```csharp
AppDbContext.cs                    // EF Core DbContext
NodeConfigRepository.cs            // implementa INodeConfigRepository
Migrations/                        // generadas con dotnet ef migrations add
InfluxDbMetricsPublisher.cs        // implementa IMetricsPublisher, usa InfluxDB.Client
PrometheusMetricsExporter.cs       // contadores y gauges via prometheus-net
InfrastructureServiceExtensions.cs // DI registration
```

### 3. TelecomOps.Api

```csharp
Program.cs                         // builder, middleware, DI
ServiceCollectionExtensions.cs     // registro limpio de servicios
Controllers/
  NodeConfigController.cs          // GET /api/nodes, POST, PUT, DELETE
  MetricsController.cs             // GET /api/metrics/{nodeId}
DTOs/
  NodeConfigRequest.cs
  NodeConfigResponse.cs
Middleware/
  ExceptionHandlingMiddleware.cs
```

### 4. TelecomOps.Worker

```csharp
TelemetryWorker.cs   // BackgroundService, cada N segundos genera TelemetryEvent
                     // por cada NodeConfig activo y lo publica via IMetricsPublisher
Program.cs
```

### 5. Tests

```csharp
// Core.Tests — unit tests puros, sin mocks de infraestructura
NodeConfigServiceTests.cs

// Api.Tests — integration tests con WebApplicationFactory + base de datos in-memory
NodeConfigControllerTests.cs
MetricsControllerTests.cs

// Worker.Tests
TelemetryWorkerTests.cs
```

### 6. GitHub Actions (`.github/workflows/ci.yml`)

- Trigger: push a `main` y pull requests
- Steps: restore → build → test → docker build (sin push)

---

## Convenciones de código

- **Async/await** en toda la capa de infraestructura y controllers. Nunca `.Result` o `.Wait()`.
- **CancellationToken** propagado en todos los métodos async, especialmente en el Worker.
- **Records** para DTOs inmutables.
- **Primary constructors** (.NET 8) donde simplifiquen el código.
- **No excepciones como control flow** — usar `Result<T>` o similar en el dominio si aplica.
- Inyección de dependencias siempre por interfaz, nunca por implementación concreta.
- `appsettings.json` para configuración, nunca hardcoded. Usar `IOptions<T>` para secciones tipadas.
- XML doc comments (`///`) en interfaces públicas.

---

## Cómo arrancar el stack

```bash
# Levantar todo (primera vez, construye imágenes)
docker compose up --build

# Levantar en background
docker compose up -d

# Ver logs de un servicio
docker compose logs -f api

# Parar y limpiar volúmenes
docker compose down -v
```

**URLs:**

| Servicio | URL | Credenciales |
|---|---|---|
| API | http://localhost:5000 | — |
| Swagger | http://localhost:5000/swagger | — |
| Grafana | http://localhost:3000 | admin / admin |
| Prometheus | http://localhost:9090 | — |
| InfluxDB | http://localhost:8086 | admin / admin123456 |

---

## Contexto para reviewers (README público)

Este proyecto demuestra intencionalmente:
- Clean Architecture aplicada a un dominio de telecomunicaciones
- Observability stack completo (no solo logging) en una app .NET
- Concurrencia correcta en BackgroundService con CancellationToken
- Tests de integración reales con WebApplicationFactory
- Docker Compose production-ready con healthchecks y non-root containers
