@echo off
setlocal enabledelayedexpansion

echo ==========================================
echo      BeatBind Publisher (Single File)
echo ==========================================
echo.

:: Check for .NET SDK
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] .NET SDK not found!
    echo Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

:: Clean previous publish folder
if exist "publish" (
    echo [INFO] Cleaning previous publish folder...
    rmdir /s /q "publish"
)

:: Run Publish Command
echo [INFO] Publishing application...
echo.
dotnet publish src/BeatBind/BeatBind.csproj -c Release -o publish

if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Publish failed!
    pause
    exit /b 1
)

echo.
echo ==========================================
echo      Publish Successful!
echo ==========================================
echo.
echo Output location: %~dp0publish\BeatBind.exe
echo.
pause
