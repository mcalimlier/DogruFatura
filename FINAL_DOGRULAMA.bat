@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

title DogruFatura 1.1 - Final Dogrulama
cls
echo ============================================================
echo DOGRU FATURA 1.1 - FINAL DOGRULAMA
echo ============================================================
echo.
echo Bu islem:
echo   - Release build ve tum otomatik testleri calistirir,
echo   - win-x64 self-contained yayini olusturur,
echo   - imzasiz FINAL SMOKE Setup paketini uretir,
echo   - uretilen Setup SHA-256 degerini tekrar dogrular.
echo.
echo UYARI: Bu komutla uretilen imzasiz Setup yalniz yerel smoke testi icindir.
echo Kullaniciya dagitim icin kod imzali final paket kullanilmalidir.
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop';" ^
  "[xml]$p=Get-Content '.\Directory.Build.props' -Raw -Encoding UTF8;" ^
  "$g=$p.Project.PropertyGroup | Select-Object -First 1;" ^
  "$v=[string]$g.VersionPrefix; $s=[string]$g.VersionSuffix;" ^
  "if($v -ne '1.1.0' -or -not [string]::IsNullOrWhiteSpace($s)){ throw ('Final surum bekleniyor. Bulunan: '+$v+'-'+$s) }"
if errorlevel 1 goto failed

echo [1/3] Release build + test + final smoke Setup hazirlaniyor...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0SETUP_HAZIRLA.ps1" -AllowUnsignedFinal
if errorlevel 1 goto failed

set "SETUP=%~dp0DAGITIM\DogruFatura_Setup_1.1.0_x64.exe"
set "HASHFILE=%~dp0DAGITIM\DogruFatura_Setup_1.1.0_x64_SHA256.txt"

echo.
echo [2/3] Dagitim dosyalari denetleniyor...
if not exist "%SETUP%" (
  echo HATA: Beklenen Setup bulunamadi:
  echo %SETUP%
  goto failed
)
if not exist "%HASHFILE%" (
  echo HATA: SHA-256 dosyasi bulunamadi:
  echo %HASHFILE%
  goto failed
)

echo [3/3] SHA-256 tekrar dogrulaniyor...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$actual=(Get-FileHash '%SETUP%' -Algorithm SHA256).Hash;" ^
  "$text=Get-Content '%HASHFILE%' -Raw;" ^
  "if($text -notmatch [regex]::Escape($actual)){ throw 'SHA-256 dosyasi Setup ile eslesmiyor.' };" ^
  "Write-Host ('[OK] SHA-256: '+$actual) -ForegroundColor Green"
if errorlevel 1 goto failed

echo.
echo ============================================================
echo [OK] FINAL SMOKE PAKETI HAZIR VE SHA-256 DOGRULANDI
echo ============================================================
echo.
echo Setup:
echo %SETUP%
echo.
echo Siradaki manuel smoke:
echo   1. Temiz kurulum
echo   2. Eski surum ustune guncelleme
echo   3. Kullanici verisinin korundugunu kontrol
echo   4. Kaldirma
echo   5. Yeniden kurulum
echo.
echo NOT: Bu paket imzasizdir; kullanici dagitimi icin kullanmayin.
echo.
pause
exit /b 0

:failed
echo.
echo ============================================================
echo [HATA] FINAL DOGRULAMA TAMAMLANAMADI
echo Yukaridaki ilk hata satirini paylasin.
echo ============================================================
echo.
pause
exit /b 1
