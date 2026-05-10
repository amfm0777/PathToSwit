# prometheus.yml — Configuration Reference

## Overview
Prometheus configuration file that defines scrape targets (where to collect metrics from) and evaluation intervals.

## Line-by-line breakdown

```yaml
global:
  scrape_interval: 15s                       # How often Prometheus scrapes targets (default for all jobs)
  evaluation_interval: 15s                   # How often to evaluate alert rules (not used yet)
  external_labels:
    monitor: 'telecomops-monitor'            # Label applied to all metrics; useful for multi-environment setups
```

### Global section
- `scrape_interval`: Prometheus queries each target every 15 seconds for metrics.
- `evaluation_interval`: For alert rules (when we add them later).
- `external_labels`: Prometheus adds `monitor="telecomops-monitor"` to every metric it scrapes.

```yaml
scrape_configs:                              # List of scrape targets
```

### Scrape config: API

```yaml
  - job_name: 'telecomops-api'               # Logical name for this job (visible in Prometheus UI)
    static_configs:                          # Static target list (no service discovery)
      - targets: ['api:8080']                # Scrape the API service at port 8080
    metrics_path: '/metrics'                 # HTTP path to the metrics endpoint (default: /metrics)
```

## What it does in practice

1. Prometheus starts and reads this config.
2. Every 15 seconds, it makes a GET request to `http://api:8080/metrics`
3. It parses the response (Prometheus text format).
4. It stores the metrics with the `monitor="telecomops-monitor"` label.
5. These metrics become queryable in Grafana.

## Adding more scrape targets

To scrape additional services, add another block:

```yaml
scrape_configs:
  - job_name: 'telecomops-api'
    static_configs:
      - targets: ['api:8080']
    metrics_path: '/metrics'
  
  - job_name: 'telecomops-worker'            # New job
    static_configs:
      - targets: ['worker:8080']             # If worker exposed metrics
    metrics_path: '/metrics'
```

## Common issues

- **Connection refused**: `api` is not running or not listening on port 8080.
- **404 Not Found**: API exists but `/metrics` endpoint is not implemented.
- **DNS failure**: Docker Compose network issue; service names must resolve.

## Reference

- Official docs: https://prometheus.io/docs/prometheus/latest/configuration/configuration/
- Metrics format: Prometheus exposition format (text-based).
