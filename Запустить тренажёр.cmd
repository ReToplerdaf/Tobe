@echo off
chcp 65001 >nul
dotnet run --project "%~dp0ToBeTrainer" -c Release
pause
