@echo off

taskkill /IM AdventurersGuild.Editor.exe /F >nul 2>&1

if errorlevel 1 (
    echo Editor was not running.
) else (
    echo Editor stopped.
)

timeout /t 2 /nobreak >nul