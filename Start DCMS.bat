@echo off
title DCMS - Faculty Payroll Portal
cd /d "%~dp0"
echo ========================================================
echo Starting DCMS (Department ^& Course Management System)
echo ========================================================
echo.
echo Starting DCMS server on http://127.0.0.1:5025...
start "DCMS Server" cmd /k "cd /d "%~dp0" && dotnet run --project DCMSApp.csproj --launch-profile http"
timeout /t 3 /nobreak >nul 2>&1
echo Opening DCMS in browser...
start "" http://127.0.0.1:5025


