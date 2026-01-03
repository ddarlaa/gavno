@echo off
echo Анализ проекта на предмет проблем с GUID...

echo.
echo Поиск файлов с проблемными GUID...

REM Поиск строк, которые могут содержать некорректные GUID
findstr /S /R /I "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}" *.cs 2>nul
if errorlevel 1 echo Нет найдено строк с экранированными GUID

findstr /S /R /I "[0-9a-fA-F]{8}[0-9a-fA-F]{4}[0-9a-fA-F]{4}[0-9a-fA-F]{4}[0-9a-fA-F]{12}" *.cs 2>nul
if errorlevel 1 echo Нет найдено строк с неправильным форматированием GUID

echo.
echo Проверка завершена.