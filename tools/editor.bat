@echo off
setlocal

set ROOT=F:\Godot_C#\Projects\AdventurersGuild
set EDITOR=%ROOT%\AdventurersGuild.Editor

echo [1/3] Stopping previous Editor instance (if any)...
taskkill /IM AdventurersGuild.Editor.exe /F >nul 2>&1
timeout /t 1 /nobreak >nul

echo [2/3] Building...
dotnet build "%EDITOR%\AdventurersGuild.Editor.csproj" -v quiet
if errorlevel 1 (
    echo.
    echo Build failed. Press any key to exit.
    pause >nul
    exit /b 1
)

echo [3/3] Starting Editor...
echo.
echo    http://localhost:5000/
echo.
echo To stop: press Ctrl+C or close this window.
echo.
cd /d "%EDITOR%"
dotnet run --no-build --project "%EDITOR%\AdventurersGuild.Editor.csproj"

endlocal