@echo off
title Fly.io DCMS Deployment
cd /d "%~dp0"
set "PATH=%USERPROFILE%\.fly\bin;%PATH%"

echo ========================================================
echo   DCMS Cloud Deployment via Fly.io
echo   Persistent 3 GB Storage for dcms.db - Mumbai Region
echo ========================================================
echo.

echo [Step 1] Checking Fly.io CLI...
where flyctl >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] flyctl not found in %USERPROFILE%\.fly\bin
    echo Please ensure flyctl is installed.
    pause
    exit /b 1
)

echo [Step 2] Checking Fly.io login...
flyctl auth whoami >nul 2>&1
if %errorlevel% equ 0 goto LOGGED_IN

echo.
echo You are not logged in to Fly.io yet.
echo Opening browser for free sign up / login...
echo.
flyctl auth login
if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Authentication was not completed.
    pause
    exit /b 1
)

:LOGGED_IN
echo.
echo Logged in successfully!
echo.

echo [Step 3] Creating 3GB persistent database volume if not existing...
flyctl volumes create dcms_data --size 3 --region bom --yes >nul 2>&1

echo.
echo [Step 4] Deploying DCMS to Fly.io cloud...
echo This will build and deploy the container automatically.
echo.
flyctl deploy --remote-only

if %errorlevel% neq 0 goto DEPLOY_FAILED

echo.
echo ========================================================
echo   DEPLOYMENT SUCCESSFUL!
echo   Opening your live web app in your browser...
echo ========================================================
flyctl open
pause
exit /b 0

:DEPLOY_FAILED
echo.
echo ========================================================
echo   Deployment failed.
echo   Check the message above for details.
echo   If the app name was taken, change 'app' in fly.toml
echo ========================================================
pause
exit /b 1
