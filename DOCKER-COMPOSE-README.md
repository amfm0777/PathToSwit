# docker-compose.yml — Complete Reference

## Overview
Orchestrates all services for local development: API, Worker, PostgreSQL, InfluxDB, Prometheus, and Grafana.

## Line-by-line breakdown

```yaml
services:
```
Defines all containers that compose this application.

### API Service

```yaml
  api:
    build:
      context: .                              # Build context is root directory
      dockerfile: src/TelecomOps.Api/Dockerfile  # Dockerfile path relative to context
    container_name: telecomops-api           # Explicit container name for easy reference
    ports:
      - "5000:8080"                          # Expose container port 8080 as localhost:5000
    environment:                             # Environment variables passed to the container
      ASPNETCORE_ENVIRONMENT: Development    # ASP.NET Core environment mode
      ConnectionStrings__Default: "Host=postgres;..."  # EF Core DB connection string
      InfluxDb__Url: "http://influxdb:8086"  # InfluxDB service URL (service name resolves via Docker DNS)
      InfluxDb__Token: "telecomops-token"    # InfluxDB auth token
      InfluxDb__Org: "telecomops"            # InfluxDB organization
      InfluxDb__Bucket: "metrics"            # InfluxDB bucket for metrics
    depends_on:                              # Startup order: wait for these to be healthy
      postgres:
        condition: service_healthy           # Wait for postgres healthcheck to pass
      influxdb:
        condition: service_healthy           # Wait for influxdb healthcheck to pass
    networks:
      - telecomops-net                       # Attach to custom network for service-to-service communication
    restart: unless-stopped                  # Auto-restart unless explicitly stopped
```

### Worker Service

```yaml
  worker:
    build:
      context: .
      dockerfile: src/TelecomOps.Worker/Dockerfile
    container_name: telecomops-worker
    environment:
      DOTNET_ENVIRONMENT: Development        # .NET environment (affects configuration loading)
      ConnectionStrings__Default: "Host=postgres;..."  # Same DB connection as API
      InfluxDb__Url: "http://influxdb:8086"
      InfluxDb__Token: "telecomops-token"
      InfluxDb__Org: "telecomops"
      InfluxDb__Bucket: "metrics"
      Worker__IntervalSeconds: "5"           # Custom config: worker runs every 5 seconds
    depends_on:
      postgres:
        condition: service_healthy           # Wait for Postgres to be ready
      influxdb:
        condition: service_healthy           # Wait for InfluxDB to be ready
    networks:
      - telecomops-net
    restart: unless-stopped
```

### PostgreSQL Service

```yaml
  postgres:
    image: postgres:16-alpine                # Official PostgreSQL 16 image (Alpine = small)
    container_name: telecomops-postgres
    ports:
      - "5432:5432"                          # PostgreSQL default port
    environment:
      POSTGRES_DB: telecomops                # Database name created automatically
      POSTGRES_USER: telecom                 # Superuser username
      POSTGRES_PASSWORD: telecom123          # Superuser password (hardcoded for dev only)
    volumes:
      - postgres-data:/var/lib/postgresql/data  # Persist database files across restarts
    healthcheck:                             # Defines how Docker checks if service is ready
      test: ["CMD-SHELL", "pg_isready -U telecom -d telecomops"]  # Check if postgres is accepting connections
      interval: 5s                           # Check every 5 seconds
      timeout: 5s                            # Timeout per check
      retries: 10                            # Max 10 failed checks before unhealthy
    networks:
      - telecomops-net
    restart: unless-stopped
```

### InfluxDB Service

```yaml
  influxdb:
    image: influxdb:2.7-alpine               # Official InfluxDB 2.7 image
    container_name: telecomops-influxdb
    ports:
      - "8086:8086"                          # InfluxDB HTTP API port
    environment:
      DOCKER_INFLUXDB_INIT_MODE: setup       # Run setup on first start
      DOCKER_INFLUXDB_INIT_USERNAME: admin   # Initial admin user
      DOCKER_INFLUXDB_INIT_PASSWORD: admin123456  # Initial admin password
      DOCKER_INFLUXDB_INIT_ORG: telecomops   # Auto-create organization
      DOCKER_INFLUXDB_INIT_BUCKET: metrics   # Auto-create bucket for metrics
      DOCKER_INFLUXDB_INIT_ADMIN_TOKEN: telecomops-token  # Auto-create API token
    volumes:
      - influxdb-data:/var/lib/influxdb2     # Persist timeseries data
    healthcheck:
      test: ["CMD", "influx", "ping"]        # Use influx CLI to check health
      interval: 10s
      timeout: 5s
      retries: 10
    networks:
      - telecomops-net
    restart: unless-stopped
```

### Prometheus Service

```yaml
  prometheus:
    image: prom/prometheus:v2.51.0           # Official Prometheus image
    container_name: telecomops-prometheus
    ports:
      - "9090:9090"                          # Prometheus web UI and API
    volumes:
      - ./infra/prometheus/prometheus.yml:/etc/prometheus/prometheus.yml:ro  # Mount config (read-only)
      - prometheus-data:/prometheus          # Persist time-series data
    command:                                 # Override default prometheus command
      - "--config.file=/etc/prometheus/prometheus.yml"  # Config file path
      - "--storage.tsdb.path=/prometheus"    # Where to store metrics
      - "--web.enable-lifecycle"             # Allow reload via HTTP API
    depends_on:
      - api                                  # Wait for API to scrape metrics from
    networks:
      - telecomops-net
    restart: unless-stopped
```

### Grafana Service

```yaml
  grafana:
    image: grafana/grafana:10.4.0             # Official Grafana image
    container_name: telecomops-grafana
    ports:
      - "3000:3000"                          # Grafana web UI
    environment:
      GF_SECURITY_ADMIN_USER: admin          # Default admin user
      GF_SECURITY_ADMIN_PASSWORD: admin      # Default admin password
      GF_USERS_ALLOW_SIGN_UP: "false"        # Disable self-registration
      GF_PATHS_PROVISIONING: /etc/grafana/provisioning  # Where to auto-load configs
    volumes:
      - ./infra/grafana/provisioning:/etc/grafana/provisioning:ro  # Provisioning configs
      - ./infra/grafana/dashboards:/var/lib/grafana/dashboards:ro  # Dashboard definitions
      - grafana-data:/var/lib/grafana        # Persist Grafana state and user data
    depends_on:
      - prometheus                           # Need Prometheus as data source
      - influxdb                             # Need InfluxDB as data source
    networks:
      - telecomops-net
    restart: unless-stopped
```

## Volumes

```yaml
volumes:
  postgres-data:                             # Named volume for PostgreSQL persistence
  influxdb-data:                             # Named volume for InfluxDB persistence
  prometheus-data:                           # Named volume for Prometheus TSDB
  grafana-data:                              # Named volume for Grafana settings
```
Named volumes are managed by Docker and survice container restarts.

## Networks

```yaml
networks:
  telecomops-net:
    driver: bridge                           # Bridge driver: containers can communicate via service names
```
Custom bridge network allows services to resolve each other by name (e.g., `postgres` → 172.20.0.2).

## Key Concepts

- **Service names as DNS**: Inside containers, `Host=postgres` resolves to the postgres container via Docker's internal DNS.
- **Healthchecks**: `depends_on: condition: service_healthy` waits for the healthcheck to pass before starting dependent services.
- **Volumes**: Ensure data persists across `docker compose restart` and `docker compose down` (without `-v`).
- **Environment Variables**: Override with `.env` file or `docker compose.override.yml` for local dev.
