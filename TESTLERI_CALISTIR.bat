@echo off
setlocal EnableExtensions
cd /d "%~dp0"

title DogruFatura 1.1 - Testler
cls
echo ============================================================
echo DOGRU FATURA 1.1 - XUNIT TEST DOGRULAMASI
echo ============================================================
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
  echo HATA: .NET SDK bulunamadi.
  echo .NET 8 veya daha yeni bir SDK gereklidir.
  echo.
  pause
  exit /b 1
)

for /f "tokens=1 delims=." %%M in ('dotnet --version 2^>nul') do set "SDK_MAJOR=%%M"
if not defined SDK_MAJOR (
  echo HATA: .NET SDK surumu okunamadi.
  pause
  exit /b 1
)
if %SDK_MAJOR% LSS 8 (
  echo HATA: .NET 8 veya daha yeni bir SDK gereklidir.
  echo Bulunan SDK:
  dotnet --version
  echo.
  pause
  exit /b 1
)

echo [OK] .NET SDK:
dotnet --version
echo.

if not exist "DogruFatura.Tests\DogruFatura.Tests.csproj" (
  echo HATA: DogruFatura.Tests projesi bulunamadi.
  pause
  exit /b 1
)

echo [1/2] Test bagimliliklari geri yukleniyor...
dotnet restore "DogruFatura.Tests\DogruFatura.Tests.csproj"
if errorlevel 1 goto test_failed

echo.
echo [2/2] Tum xUnit testleri calistiriliyor...
dotnet test "DogruFatura.Tests\DogruFatura.Tests.csproj" -c Debug --no-restore --logger "console;verbosity=normal"
if errorlevel 1 goto test_failed

echo.
echo ============================================================
echo [OK] TUM TESTLER BASARILI
echo ============================================================
echo.
pause
exit /b 0

:test_failed
echo.
echo ============================================================
echo [HATA] TEST DOGRULAMASI BASARISIZ
echo Yukaridaki ilk hata veya basarisiz test satirlarini paylasin.
echo ============================================================
echo.
pause
exit /b 1
