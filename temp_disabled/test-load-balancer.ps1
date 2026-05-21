Write-Host "=== Тест балансировки нагрузки IceBreakerApp ===" -ForegroundColor Cyan

$url = "https://localhost/api/test/instance-info"
$requests = 10

Write-Host "`nОтправка $requests запросов к $url..." -ForegroundColor Yellow
Write-Host "================================================" -ForegroundColor Gray

# Отключаем проверку SSL для самоподписанного сертификата
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

$instanceCount = @{}

for ($i = 1; $i -le $requests; $i++) {
    try {
        $response = Invoke-RestMethod -Uri $url -Method Get
        $instanceId = $response.instanceId
        
        # Подсчёт запросов по инстансам
        if ($instanceCount.ContainsKey($instanceId)) {
            $instanceCount[$instanceId]++
        } else {
            $instanceCount[$instanceId] = 1
        }
        
        Write-Host "Запрос $i : $instanceId | Hostname: $($response.containerHostname)" -ForegroundColor Green
    }
    catch {
        Write-Host "Запрос $i : ОШИБКА - $_" -ForegroundColor Red
    }
    
    Start-Sleep -Milliseconds 100
}

Write-Host "`n================================================" -ForegroundColor Gray
Write-Host "Распределение запросов:" -ForegroundColor Yellow
foreach ($instance in $instanceCount.Keys | Sort-Object) {
    $count = $instanceCount[$instance]
    $percent = ($count / $requests) * 100
    $bar = "█" * [Math]::Round($percent / 5)
    Write-Host "  $instance : $count запросов ($percent%) $bar" -ForegroundColor Cyan
}

Write-Host "`n=== Тест статических файлов ===" -ForegroundColor Cyan
try {
    $staticResponse = Invoke-WebRequest -Uri "https://localhost/static/test.html"
    Write-Host "✅ Статический файл доступен! Статус: $($staticResponse.StatusCode)" -ForegroundColor Green
    Write-Host "   Content-Type: $($staticResponse.Headers['Content-Type'])" -ForegroundColor Gray
    Write-Host "   Размер: $($staticResponse.RawContentLength) байт" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Ошибка доступа к статическому файлу: $_" -ForegroundColor Red
}

Write-Host "`n=== Проверка прямого доступа к инстансам ===" -ForegroundColor Cyan
$directUrls = @("http://localhost:5000", "http://localhost:5001", "http://localhost:5002")
foreach ($url in $directUrls) {
    try {
        $response = Invoke-WebRequest -Uri $url -Method Get -TimeoutSec 2
        Write-Host "⚠️  $url доступен напрямую (не должно быть!)" -ForegroundColor Yellow
    }
    catch {
        Write-Host "✅ $url недоступен (правильно)" -ForegroundColor Green
    }
}

Write-Host "`n=== Тест завершён ===" -ForegroundColor Cyan