@echo off
echo TelecomOps Platform Startup Script
echo.
echo Options:
echo 1. Start in foreground (with logs)
echo 2. Start in background
echo 3. View API logs
echo 4. View Worker logs
echo 5. Stop and clean
echo.

set /p choice="Choose an option (1-5): "

if "%choice%"=="1" (
    echo Starting in foreground...
    docker compose up --build
) else if "%choice%"=="2" (
    echo Starting in background...
    docker compose up -d --build
    echo Services started. Use 'docker compose logs -f [service]' to view logs.
) else if "%choice%"=="3" (
    echo Viewing API logs...
    docker compose logs -f api
) else if "%choice%"=="4" (
    echo Viewing Worker logs...
    docker compose logs -f worker
) else if "%choice%"=="5" (
    echo Stopping and cleaning...
    docker compose down -v
) else (
    echo Invalid option.
)

pause