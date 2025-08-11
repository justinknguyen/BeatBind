@echo off
echo Building BeatBind C# Application (Clean Architecture)...
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

echo Navigating to Clean Architecture source directory...
cd src

echo Restoring NuGet packages...
dotnet restore BeatBind.sln
if %errorlevel% neq 0 (
    echo Failed to restore packages!
    pause
    exit /b 1
)

echo.
echo Building Clean Architecture solution...
dotnet build BeatBind.sln --configuration Release
if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Build successful!
echo You can run the application with: cd BeatBind && dotnet run
echo Or find the executable in: BeatBind\bin\Release\net8.0-windows\
echo.
echo Clean Architecture Structure:
echo   Domain Layer:        BeatBind.Domain
echo   Application Layer:   BeatBind.Application  
echo   Infrastructure Layer: BeatBind.Infrastructure
echo   Presentation Layer:  BeatBind.Presentation
echo   Main Application:    BeatBind
echo.
pause
echo.
pause
