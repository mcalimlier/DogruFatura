@echo off
setlocal EnableExtensions
chcp 65001 >nul
cd /d "%~dp0"

title DogruFatura 1.1 - Final EXE Olustur
cls
echo ============================================================
echo DOGRU FATURA 1.1 - FINAL EXE OLUSTUR
echo ============================================================
echo.

if "%DOGRUFATURA_SIGN_CERT_SHA1%"=="" (
    echo [BILGI] Kod imzalama sertifikasi bulunamadi.
    echo [UYARI] Kullanici karariyla IMZASIZ final dagitim paketi uretilecek.
    echo [UYARI] Windows SmartScreen / Bilinmeyen yayinci uyarisi gosterebilir.
    echo.
    set "SIGNMODE=IMZASIZ"
    set "SETUP_ARGS=-AllowUnsignedDistribution"
) else (
    echo [BILGI] Kod imzalama sertifikasi tanimli; final paket imzalanacak.
    echo.
    set "SIGNMODE=IMZALI"
    set "SETUP_ARGS="
)

echo [1/3] Final Setup hazirlaniyor...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0SETUP_HAZIRLA.ps1" %SETUP_ARGS%
if errorlevel 1 goto failed

set "SETUP=%~dp0DAGITIM\DogruFatura_Setup_1.1.0_x64.exe"
set "HASHFILE=%~dp0DAGITIM\DogruFatura_Setup_1.1.0_x64_SHA256.txt"

echo.
echo [2/3] Final dosyalar denetleniyor...
if not exist "%SETUP%" (
    echo [HATA] Setup bulunamadi:
    echo %SETUP%
    goto failed
)
if not exist "%HASHFILE%" (
    echo [HATA] SHA-256 dosyasi bulunamadi:
    echo %HASHFILE%
    goto failed
)

echo [3/3] SHA-256 tekrar dogrulaniyor...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command ^
  "$actual=(Get-FileHash '%SETUP%' -Algorithm SHA256).Hash;" ^
  "$text=Get-Content '%HASHFILE%' -Raw;" ^
  "if($text -notmatch [regex]::Escape($actual)){ throw 'SHA-256 dosyasi Setup ile eslesmiyor.' };" ^
  "Write-Host ('[OK] SHA-256: '+$actual) -ForegroundColor Green"
if errorlevel 1 goto failed

echo.
echo ============================================================
echo [OK] %SIGNMODE% FINAL EXE HAZIR VE SHA-256 DOGRULANDI
echo ============================================================
echo.
echo Setup:
echo %SETUP%
echo.
if "%SIGNMODE%"=="IMZASIZ" (
    echo NOT: Dagitimda EXE ile birlikte SHA-256 dosyasini da verin.
    echo NOT: SmartScreen / Bilinmeyen yayinci uyarisi normaldir.
)
echo.
echo DAGITIM klasoru aciliyor...
start "" "%~dp0DAGITIM"
echo.
pause
exit /b 0

:failed
echo.
echo ============================================================
echo [HATA] FINAL EXE OLUSTURULAMADI
echo Yukaridaki ilk hata satirini paylasin.
echo ============================================================
echo.
pause
exit /b 1
