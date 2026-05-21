# build-and-run.ps1

# 1. Очистка предыдущей публикации (опционально)
if (Test-Path ./publish) {
    Remove-Item -Recurse -Force ./publish
}

# 2. Локальная публикация API проекта
Write-Host "Building API locally..." -ForegroundColor Green
dotnet publish ./API/API.csproj -c Release -o ./publish

# 3. Проверка, что сборка прошла успешно
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

# 4. Сборка Docker-образа
Write-Host "Building Docker image..." -ForegroundColor Green
docker build -t icebreakerapp-api -f Dockerfile .

# 5. Запуск через docker-compose (если вы используете compose)
Write-Host "Starting containers..." -ForegroundColor Green
docker-compose up -d

Write-Host "Done!" -ForegroundColor Green