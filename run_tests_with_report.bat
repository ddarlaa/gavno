@echo off
echo ========================================
echo IceBreakerApp - JWT Testing Suite
echo ========================================
echo.

REM Создаем директорию для отчетов если не существует
if not exist "TestResults" mkdir TestResults

REM Очищаем предыдущие отчеты
del /Q TestResults\*.html TestResults\*.trx TestResults\*.xml 2>nul

echo Running unit tests with detailed reporting...
echo.

REM Запускаем тесты с генерацией отчетов
dotnet test Tests/Tests.csproj ^
    --logger "console;verbosity=detailed" ^
    --logger "html;LogFileName=TestResults\test_report.html" ^
    --logger "trx;LogFileName=TestResults\test_results.trx" ^
    --logger "xunit;LogFileName=TestResults\test_results.xml" ^
    --collect:"XPlat Code Coverage" ^
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura ^
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByAttribute=Obsolete,GeneratedCodeAttribute ^
    -- DataCollectionRunSettings.DataCollectors.DataCollector.ConfigurationExcludeByFile=*Designer.cs

echo.
echo ========================================
echo Test execution completed!
echo ========================================
echo.
echo Reports generated:
echo - HTML Report: TestResults\test_report.html
echo - TRX Report: TestResults\test_results.trx  
echo - XML Report: TestResults\test_results.xml
echo - Coverage: TestResults\coverage.cobertura.xml
echo.
echo Opening HTML report...
start TestResults\test_report.html

pause