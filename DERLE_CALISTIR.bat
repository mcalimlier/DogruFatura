@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

title DogruFatura 1.0 - Asama 10 R2
cls
echo ============================================================
echo DOGRU FATURA 1.0 - ASAMA 10 R2
echo ============================================================
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
  echo HATA: .NET SDK bulunamadi.
  echo Bu kaynak paketini calistirmak icin .NET 8 SDK kurulmalidir.
  echo https://dotnet.microsoft.com/download/dotnet/8.0
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
if !SDK_MAJOR! LSS 8 (
  echo HATA: .NET 8 veya daha yeni bir SDK gereklidir.
  echo Bulunan SDK:
  dotnet --version
  echo.
  pause
  exit /b 1
)
echo [OK] .NET SDK: 
dotnet --version

echo [1/3] Kaynaklar denetleniyor...
if not exist "DogruFatura.Wpf\DogruFatura.Wpf.csproj" goto missing_source
if not exist "DogruFatura.Core\DogruFatura.Core.csproj" goto missing_source
if not exist "DogruFatura.Wpf\Assets\AppIcon.ico" goto missing_source

echo [2/3] Proje derleniyor...
dotnet restore "DogruFatura.Wpf\DogruFatura.Wpf.csproj"
if errorlevel 1 goto build_failed
dotnet build "DogruFatura.Wpf\DogruFatura.Wpf.csproj" -c Debug --no-restore
if errorlevel 1 goto build_failed

echo [3/3] DogruFatura baslatiliyor...
dotnet run --project "DogruFatura.Wpf\DogruFatura.Wpf.csproj" -c Debug --no-build
exit /b %errorlevel%

:missing_source
echo HATA: Kaynak dosyalardan biri eksik. ZIP'i yeniden acin.
pause
exit /b 1

:build_failed
echo.
echo HATA: Derleme tamamlanamadi. Yukaridaki ilk hata satirini paylasin.
pause
exit /b 1
