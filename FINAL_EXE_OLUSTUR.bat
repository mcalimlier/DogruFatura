@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0"

echo ============================================================
echo DOGRU FATURA 1.1 - FINAL IMZALI EXE OLUSTUR
echo ============================================================
echo.

if "%DOGRUFATURA_SIGN_CERT_SHA1%"=="" (
    echo [HATA] Kod imzalama sertifikasi tanimli degil.
    echo.
    echo Bu arac yalniz SON KULLANICIYA DAGITILACAK IMZALI FINAL EXE icindir.
    echo Mevcut imzasiz smoke EXE zaten:
    echo   DAGITIM\DogruFatura_Setup_1.1.0_x64.exe
    echo.
    echo Imzali final icin Windows sertifika deposunda kod imzalama
    echo sertifikasi bulunmali ve DOGRUFATURA_SIGN_CERT_SHA1 tanimli olmali.
    echo.
    pause
    exit /b 2
)

echo [1/2] Imzali final Setup hazirlaniyor...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0SETUP_HAZIRLA.ps1"
if errorlevel 1 (
    echo.
    echo [HATA] Final imzali EXE olusturulamadi.
    pause
    exit /b 1
)

echo.
echo [2/2] Final paket hazir.
echo.
echo DAGITIM klasoru aciliyor...
start "" "%~dp0DAGITIM"

echo.
echo ============================================================
echo [OK] IMZALI FINAL EXE HAZIR
echo ============================================================
echo.
pause
exit /b 0
