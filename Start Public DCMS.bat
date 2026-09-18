@echo off
title DCMS Worldwide Public Server (Cloudflare HTTPS Tunnel)
cd /d "%~dp0"

echo ========================================================
echo   DCMS PUBLIC CLOUD DEPLOYMENT (100%% FREE - NO CARD)
echo   MIT-WPU x NIRVAA Faculty Payment Portal
echo ========================================================
echo.

:: 1. Check if port 5025 is already running
netstat -ano | findstr 127.0.0.1:5025 >nul 2>&1
if %errorlevel% equ 0 goto APP_RUNNING

echo [1/2] Starting DCMS Backend in Production Mode...
if exist "publish\DCMSApp.exe" (
    start "DCMS App Server" /d "%~dp0publish" "%~dp0publish\DCMSApp.exe" --urls "http://0.0.0.0:5025"
) else (
    start "DCMS App Server" dotnet run -c Release --urls "http://0.0.0.0:5025"
)
timeout /t 4 /nobreak >nul
goto START_TUNNEL

:APP_RUNNING
echo [1/2] DCMS Backend is already running on port 5025.

:START_TUNNEL
echo.
echo [2/2] Connecting to Cloudflare Global Edge Network...
echo.
echo ========================================================
echo   Look for your worldwide public HTTPS link below:
echo   It will look like:  https://xxxxxx.trycloudflare.com
echo.
echo   Share this link with anyone anywhere on phone or PC!
echo   (Press Ctrl+C to stop the public server)
echo ========================================================
echo.

cloudflared.exe tunnel --url http://127.0.0.1:5025

pause
