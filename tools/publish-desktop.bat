@echo off
setlocal
cd /d "%~dp0"

echo Stopping Desktop if running...
taskkill /IM AdventurersGuild.Desktop.exe /F >nul 2>&1
timeout /t 1 /nobreak >nul

echo Building and publishing...
dotnet publish AdventurersGuild.Desktop -c Release -r win-x64 --self-contained false -o publish

if errorlevel 1 (
    echo Publish FAILED.
    pause >nul
    exit /b 1
)

echo.
echo Publishing done: publish\AdventurersGuild.Desktop.exe
echo.
pause >nul