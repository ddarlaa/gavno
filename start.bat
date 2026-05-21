@echo off
echo ========================================
echo   Лабораторная работа №3: Docker и Docker Compose
echo   Запуск IceBreakerApp
echo ========================================
echo.

echo [1/4] Проверка наличия Docker Compose...
docker-compose --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ОШИБКА] Docker Compose не найден!
    echo Убедитесь, что Docker Desktop установлен и запущен.
    pause
    exit /b 1
)
echo [OK] Docker Compose найден
echo.

echo [2/4] Запуск контейнеров...
docker-compose up -d --build
if %errorlevel% neq 0 (
    echo [ОШИБКА] Не удалось запустить контейнеры!
    pause
    exit /b 1
)
echo [OK] Контейнеры запущены
echo.

echo [3/4] Проверка статуса контейнеров...
docker-compose ps
echo.

echo [4/4] Ожидание инициализации базы данных (5 секунд)...
timeout /t 5 /nobreak >nul
echo.

echo ========================================
echo   [ГОТОВО] Сервис успешно запущен!
echo ========================================
echo.
echo   API: http://localhost:5000
echo   Swagger: http://localhost:5000/swagger
echo   PostgreSQL: localhost:5432
echo.
echo   Для просмотра логов выполните:
echo     docker-compose logs -f
echo.
echo   Для остановки выполните:
echo     docker-compose down
echo.
pause