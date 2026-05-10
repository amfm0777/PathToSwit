# TelecomOps.Api — HTTP REST API Layer

## Purpose

ASP.NET Core 8 REST API for configuring and monitoring 5G nodes. Exposes endpoints for CRUD operations and telemetry queries.

## Key Components

### Program.cs

- Configures DbContext with PostgreSQL
- Registers services: repositories, services, metrics publisher
- Ensures database schema exists on startup
- Configures Swagger/OpenAPI documentation
- Maps HTTP endpoints

### Endpoints

#### GET `/`
- Returns: `"TelecomOps API"`
- Purpose: Health check

#### GET `/nodes`
- Returns: List of all NodeConfig objects
- Use: Get all nodes in the system

#### GET `/nodes/active`
- Returns: List of active NodeConfig objects
- Use: Query only operational nodes

#### POST `/nodes`
- Parameters: `name` (string), `frequencyBand` (FrequencyBand enum)
- Returns: Created NodeConfig with 201 status
- Use: Create a new network node

### DTOs (if added)

Not yet implemented. Later can add:
- `NodeConfigRequest`: Input validation
- `NodeConfigResponse`: Output formatting
- `TelemetryEventResponse`: Metrics response

### Middleware

Not yet implemented. Later can add:
- `ExceptionHandlingMiddleware`: Global error handling
- Logging middleware
- CORS middleware

### Swagger

Available at `http://localhost:5000/swagger`
- Auto-generated from endpoint definitions
- Test endpoints interactively
- View request/response schemas

## Configuration

```csharp
// appsettings.json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=telecomops;..."
  },
  "InfluxDb": {
    "Url": "http://localhost:8086",
    "Token": "...",
    "Org": "telecomops",
    "Bucket": "metrics"
  }
}
```

## Dependencies

- `Microsoft.EntityFrameworkCore`
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `InfluxDB.Client`
- `Swashbuckle.AspNetCore` (Swagger)

## Dependency Injection

- `INodeConfigRepository`: Scoped (one per request)
- `INodeConfigService`: Scoped
- `IMetricsPublisher`: Singleton
- `AppDbContext`: Scoped

## Running

```bash
# Local development
dotnet run --project src/TelecomOps.Api/

# In Docker
docker compose up api
```

## Testing endpoints

### Create a node
```bash
curl -X POST "http://localhost:5000/nodes?name=Node-1&frequencyBand=3500" \
  -H "Content-Type: application/json"
```

### Get all nodes
```bash
curl "http://localhost:5000/nodes"
```

### Get active nodes
```bash
curl "http://localhost:5000/nodes/active"
```

## Why this layer exists

- **HTTP exposure**: Makes domain logic accessible via REST.
- **Request handling**: ASP.NET Core handles routing, binding, serialization.
- **Swagger**: Auto-documents API for consumers.
- **Separation**: API layer independent of Worker or other clients.
