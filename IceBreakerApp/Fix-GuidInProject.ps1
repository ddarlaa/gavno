# Скрипт для исправления GUID в проекте
param(
    [string]$Path = "C:\Users\dpugi\RiderProjects\IceBreakerApp"
)

Write-Host "Анализ проекта на предмет проблем с GUID..." -ForegroundColor Yellow

# Функция для проверки и исправления GUID
function Fix-GuidInFile {
    param(
        [string]$FilePath
    )
    
    $content = Get-Content $FilePath -Raw
    $originalContent = $content
    $issuesFound = @()
    
    # Паттерны для поиска проблемных GUID
    $patterns = @(
        @{
            Pattern = '([0-9a-fA-F]{8})\\{4}([0-9a-fA-F]{4})\\{4}([0-9a-fA-F]{4})\\{4}([0-9a-fA-F]{12})'
            Replacement = '$1-$2-$3-$4'
            Description = "Экранированные обратные слеши"
        },
        @{
            Pattern = '([0-9a-fA-F]{8})([0-9a-fA-F]{4})([0-9a-fA-F]{4})([0-9a-fA-F]{4})([0-9a-fA-F]{12})'
            Replacement = '$1-$2-$3-$4'
            Description = "Отсутствующие дефисы"
        }
    )
    
    foreach ($pattern in $patterns) {
        $matches = [regex]::Matches($content, $pattern.Pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        
        if ($matches.Count -gt 0) {
            foreach ($match in $matches) {
                $oldGuid = $match.Value
                $newGuid = [regex]::Replace($oldGuid, $pattern.Pattern, $pattern.Replacement)
                
                # Проверяем, что новый GUID корректный
                try {
                    $guidTest = [guid]::Parse($newGuid)
                    $content = $content -replace [regex]::Escape($oldGuid), $newGuid
                    $issuesFound += "Заменено: $oldGuid -> $newGuid ($($pattern.Description))"
                }
                catch {
                    Write-Host "ОШИБКА: Не удалось создать корректный GUID из: $oldGuid" -ForegroundColor Red
                }
            }
        }
    }
    
    # Проверяем на наличие дублированных дефисов
    $doubleDashMatches = [regex]::Matches($content, '--+')
    if ($doubleDashMatches.Count -gt 0) {
        foreach ($match in $doubleDashMatches) {
            $content = $content -replace '--+', '-'
            $issuesFound += "Исправлены дублированные дефисы"
        }
    }
    
    # Сохраняем изменения только если были найдены проблемы
    if ($issuesFound.Count -gt 0 -and $content -ne $originalContent) {
        Set-Content -Path $FilePath -Value $content -Encoding UTF8
        Write-Host "Файл обновлен: $FilePath" -ForegroundColor Green
        foreach ($issue in $issuesFound) {
            Write-Host "  - $issue" -ForegroundColor Yellow
        }
        return $true
    }
    
    return $false
}

# Поиск всех C# файлов в проекте
$csFiles = Get-ChildItem -Path $Path -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue

Write-Host "Найдено $($csFiles.Count) C# файлов для анализа" -ForegroundColor Cyan

$updatedFiles = 0
$totalIssues = 0

foreach ($file in $csFiles) {
    Write-Host "Анализ: $($file.FullName)" -ForegroundColor Gray
    if (Fix-GuidInFile -FilePath $file.FullName) {
        $updatedFiles++
    }
}

Write-Host "`nРезультат:" -ForegroundColor Yellow
Write-Host "Обновлено файлов: $updatedFiles" -ForegroundColor Green
Write-Host "Анализ завершен!" -ForegroundColor Green