@echo off
setlocal

cd /d "%~dp0.."
echo [DEBUG] Current dir: %cd%
set ROOT=%cd%
set EDITOR=%ROOT%\src\AdventurersGuild.Editor
echo [DEBUG] ROOT   = %ROOT%
echo [DEBUG] EDITOR = %EDITOR%
echo.

echo [DEBUG] Checking csproj exists...
if not exist "%EDITOR%\AdventurersGuild.Editor.csproj" (
    echo [ERROR] csproj NOT found at: %EDITOR%\AdventurersGuild.Editor.csproj
    echo.
    echo Listing contents of src:
    dir /b "%ROOT%\src"
    pause
    exit /b 1
)
echo [DEBUG] csproj found OK.
echo.

echo [DEBUG] Killing old process (if any)...
taskkill /IM AdventurersGuild.Editor.exe /F
echo [DEBUG] taskkill errorlevel: %errorlevel%
timeout /t 1 /nobreak >nul
echo.

echo [DEBUG] Building...
dotnet build "%EDITOR%\AdventurersGuild.Editor.csproj"
echo [DEBUG] build errorlevel: %errorlevel%
if errorlevel 1 (
    echo.
    echo [ERROR] Build failed.
    pause
    exit /b 1
)
echo.

echo [DEBUG] Starting Editor...
cd /d "%EDITOR%"
dotnet run --no-build --project "%EDITOR%\AdventurersGuild.Editor.csproj"
echo [DEBUG] dotnet run errorlevel: %errorlevel%

pause
endlocal