@echo off
echo Publishing ZebraPrinterCLI solution...
dotnet publish ZebraPrinterCLI/ZebraPrinterCLI.sln -c Release -o ./publish

if %errorlevel% neq 0 (
    echo Publishing failed with error code %errorlevel%.
    pause
    exit /b %errorlevel%
)

echo Publishing succeeded.
pause
