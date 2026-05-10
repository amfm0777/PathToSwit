# Docker Cheat Sheet

## Common commands

- `docker compose up --build`
  - Start all services and rebuild images.
- `docker compose up -d --build`
  - Start in detached mode and rebuild images.
- `docker compose up -d`
  - Start in detached mode without rebuilding.
- `docker compose down`
  - Stop services and remove the network, but keep volumes.
- `docker compose down -v`
  - Stop services and remove associated volumes.
- `docker compose ps`
  - List running compose services.
- `docker compose logs -f api`
  - Tail logs from the `api` service.
- `docker compose logs -f worker`
  - Tail logs from the `worker` service.
- `docker compose build api`
  - Rebuild only the `api` image.
- `docker compose build worker`
  - Rebuild only the `worker` image.
- `docker compose exec api bash`
  - Open a shell inside the `api` container.
- `docker compose exec postgres psql -U telecom -d telecomops`
  - Open a Postgres shell inside the `postgres` container.

## Quick notes

- `--build` forces image rebuild, which reruns `dotnet restore` inside the Dockerfile.
- `-d` runs containers in the background.
- `down -v` removes volume data, so use it carefully in development.
- Prefer `docker compose up -d` for stable repeated runs once the images are built.

## Useful workflow

1. Build and start once:
   - `docker compose up -d --build`
2. Restart normally after code changes:
   - `docker compose up -d`
3. Stop and clean completely:
   - `docker compose down -v`
4. View logs while debugging:
   - `docker compose logs -f worker`
   - `docker compose logs -f api`
