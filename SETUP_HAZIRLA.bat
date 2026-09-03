@echo off
setlocal EnableExtensions
cd /d "%~dp0"

cls
echo ============================================================
echo DOGRU FATURA SETUP HAZIRLAMA
 echo ============================================================
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0SETUP_HAZIRLA.ps1"
set "EXITCODE=%ERRORLEVEL%"

echo.
if "%EXITCODE%"=="2" (
  echo RC/test surumunde final Setup bilincli olarak uretilmedi.
) else if not "%EXITCODE%"=="0" (
  echo Setup hazirlama islemi basarisiz oldu. Hata kodu: %EXITCODE%
)
echo.
pause
exit /b %EXITCODE%
