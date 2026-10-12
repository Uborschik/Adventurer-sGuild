@echo off
setlocal

cd /d "F:\Godot_C#\Projects\AdventurersGuild"
dotnet build AdventurersGuild.csproj

echo.
echo Done. Press any key to exit.
pause >nul

endlocal