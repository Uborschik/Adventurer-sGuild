@echo off
setlocal

cd /d "%~dp0.."
echo Building Godot project...
dotnet build src\AdventurersGuild.Game\AdventurersGuild.csproj

if errorlevel 1 (
    echo.
    echo Build FAILED.
    pause >nul
    exit /b 1
)

echo.
echo Done. Press any key to exit.
pause >nul

endlocal