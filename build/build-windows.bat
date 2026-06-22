@echo off
REM Build script for Windows
echo ========================================
echo  RM-Sweep - Building for Windows
echo ========================================

echo.
echo [1/2] Building self-contained Windows x64 executable...
dotnet publish src\RMSweep\RMSweep.csproj -c Release -r win-x64 --self-contained true -o output\win-x64

if %ERRORLEVEL% neq 0 (
    echo.
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo [2/2] Build complete!
echo Output: output\win-x64\RMSweep.exe
echo.
echo To create an installer, use Inno Setup with build\installer.iss
echo.
pause
