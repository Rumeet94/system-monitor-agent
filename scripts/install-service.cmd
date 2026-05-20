@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%install-service.ps1" %*
set "EXIT_CODE=%ERRORLEVEL%"
echo.
if "%EXIT_CODE%"=="0" (
    echo Operation completed successfully.
) else (
    echo Operation failed. Exit code: %EXIT_CODE%.
)
echo Press any key to close this window...
pause >nul
exit /b %EXIT_CODE%
