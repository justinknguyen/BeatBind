@echo off
echo Building BeatBind C# Application...
echo.

echo Checking for .NET SDK...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo .NET SDK not found!
    echo Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

echo .NET SDK found!
echo.

echo Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo Failed to restore packages!
    pause
    exit /b 1
)

echo.
echo Building application...
dotnet build --configuration Release
if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Build successful!
echo You can run the application with: dotnet run
echo Or find the executable in: bin\Release\net8.0-windows\
echo.
pause
