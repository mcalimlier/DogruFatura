Unicode true

!include "MUI2.nsh"
!include "x64.nsh"
!include "LogicLib.nsh"
!include "WinVer.nsh"
!include "WordFunc.nsh"

!insertmacro VersionCompare

!define APPNAME "DoğruFatura"
!define APPVERSION "1.0.0"
!define APPEXENAME "DogruFatura.exe"
!define APPDESC "Fatura Ödeme Listesi Hazırlama ve Kontrol Programı"
!define APPKEY "Software\DoğruFatura"
!define UNINSTKEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\DogruFatura.Desktop.2D22EB36-5AA7-4AD9-9814-DB03D1DFBFD3"

Name "${APPNAME} ${APPVERSION}"
OutFile "..\_YAYIN\Setup\DogruFatura_Setup_1.0.0_x64.exe"
InstallDir "$PROGRAMFILES64\DoğruFatura"
InstallDirRegKey HKLM "${APPKEY}" "InstallDir"
RequestExecutionLevel admin
CRCCheck force
SetCompressor /SOLID lzma
SetCompressorDictSize 32
ShowInstDetails show
ShowUninstDetails show
BrandingText "DoğruFatura ${APPVERSION}"

VIProductVersion "1.0.0.0"
VIAddVersionKey /LANG=1055 "ProductName" "DoğruFatura"
VIAddVersionKey /LANG=1055 "FileDescription" "DoğruFatura Kurulum Programı"
VIAddVersionKey /LANG=1055 "FileVersion" "1.0.0.0"
VIAddVersionKey /LANG=1055 "ProductVersion" "1.0.0"
VIAddVersionKey /LANG=1055 "CompanyName" "DoğruFatura"
VIAddVersionKey /LANG=1055 "LegalCopyright" "DoğruFatura"

!define MUI_ICON "..\DogruFatura.Wpf\Assets\AppIcon.ico"
!define MUI_UNICON "..\DogruFatura.Wpf\Assets\AppIcon.ico"
!define MUI_ABORTWARNING
!define MUI_WELCOMEPAGE_TITLE "DoğruFatura ${APPVERSION} Kurulumuna Hoş Geldiniz"
!define MUI_WELCOMEPAGE_TEXT "ÖNEMLİ BİLGİ: Windows bazı bilgisayarlarda kurulum dosyasını ilk açarken SmartScreen / Bilinmeyen yayıncı uyarısı gösterebilir. Dosyayı kurumunuzdan veya güvenilir kaynaktan aldıysanız seçenek sunuluyorsa Daha fazla bilgi > Yine de çalıştır ile devam edebilirsiniz.$\r$\n$\r$\nDoğruFatura Windows 10/11 64-bit için self-contained hazırlanmıştır. Hedef bilgisayarda .NET SDK, .NET Runtime, Visual Studio veya Excel gerekmez.$\r$\n$\r$\nMevcut DoğruFatura kullanıcı verileri, yedekleri ve PDF arşivi korunur."
!define MUI_FINISHPAGE_TITLE "DoğruFatura Kurulumu Tamamlandı"
!define MUI_FINISHPAGE_TEXT "DoğruFatura başarıyla kuruldu.$\r$\n$\r$\nKullanıcı verileri, yedekler, loglar ve PDF belgeleri Masaüstü\DoğruFatura klasöründe birlikte tutulur. Program kaldırıldığında bu kayıtlar silinmez."
!define MUI_FINISHPAGE_RUN "$INSTDIR\${APPEXENAME}"
!define MUI_FINISHPAGE_RUN_TEXT "DoğruFatura'yı şimdi çalıştır"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "Turkish"

Function .onInit
  ${IfNot} ${AtLeastWin10}
    MessageBox MB_ICONSTOP|MB_OK "DoğruFatura yalnızca Windows 10/11 64-bit bilgisayarlarda desteklenir."
    Abort
  ${EndIf}

  ${IfNot} ${RunningX64}
    MessageBox MB_ICONSTOP|MB_OK "DoğruFatura yalnızca Windows 10/11 64-bit bilgisayarlarda çalışır."
    Abort
  ${EndIf}

  SetRegView 64

  ; Temiz kurulumda, sihirbaz açılmadan hemen önce kısa bir kullanıcı bilgilendirmesi göster.
  ; Sessiz /S kurulumda CI ve kurumsal dağıtımın takılmaması için bu bilgilendirme atlanır.
  ReadRegStr $0 HKLM "${UNINSTKEY}" "DisplayVersion"
  IfSilent skip_clean_install_info
  ${If} $0 == ""
    MessageBox MB_ICONINFORMATION|MB_OK "Kurulum bilgisi:$\r$\n$\r$\nWindows bazı bilgisayarlarda DoğruFatura kurulum dosyasını ilk açarken SmartScreen / Bilinmeyen yayıncı uyarısı gösterebilir.$\r$\n$\r$\nDosyayı kurumunuzdan veya güvenilir kaynaktan aldıysanız seçenek sunuluyorsa Daha fazla bilgi > Yine de çalıştır ile devam edebilirsiniz."
  ${EndIf}
skip_clean_install_info:

  ; Daha yeni bir sürüm zaten kuruluysa yanlışlıkla geri sürüme dönmeyi engelle.
  ${If} $0 != ""
    ${VersionCompare} "$0" "${APPVERSION}" $1
    ${If} $1 == 1
      MessageBox MB_ICONSTOP|MB_OK "Bu bilgisayarda DoğruFatura $0 sürümü zaten kurulu.$\r$\n$\r$\nKurmak istediğiniz ${APPVERSION} sürümü daha eskidir. Veri ve program uyumluluğunu korumak için kurulum durduruldu."
      Abort
    ${EndIf}
  ${EndIf}
FunctionEnd

Function CloseDogruFatura
  ; Önceki sürüm çalışıyorsa sessizce kapat ve Windows'un DLL kilitlerini bırakması için bekle.
  nsExec::ExecToStack '"$SYSDIR\taskkill.exe" /IM "${APPEXENAME}" /T /F'
  Pop $0
  Pop $1
  Sleep 2500
FunctionEnd

Function CheckInstallEnvironment
  ; Program Files hedef sürücüsünde en az 500 MB boş alan iste.
  ; PowerShell Windows 10/11'in yerleşik bileşenidir. Kontrol çalışamazsa kurulum devam eder;
  ; yalnız gerçekten yetersiz alan saptanırsa durdurulur.
  nsExec::ExecToStack `"$SYSDIR\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -Command "if((New-Object System.IO.DriveInfo ([System.IO.Path]::GetPathRoot('$INSTDIR'))).AvailableFreeSpace -ge 524288000){exit 0}else{exit 2}"`
  Pop $0
  Pop $1
  ${If} $0 == 2
    MessageBox MB_ICONSTOP|MB_OK "Kurulum için yeterli boş disk alanı yok.$\r$\n$\r$\nDoğruFatura kurulumu için hedef sürücüde en az 500 MB boş alan bırakın ve kurulumu yeniden çalıştırın."
    Abort
  ${EndIf}

  ; Yönetici yetkisi alınmış olsa bile Program Files hedefinin gerçekten yazılabildiğini doğrula.
  CreateDirectory "$INSTDIR"
  ClearErrors
  FileOpen $2 "$INSTDIR\.dogrufatura_write_test.tmp" w
  IfErrors write_test_failed
  FileWrite $2 "DoğruFatura kurulum yazma testi"
  FileClose $2
  Delete "$INSTDIR\.dogrufatura_write_test.tmp"
  Return

write_test_failed:
  MessageBox MB_ICONSTOP|MB_OK "Kurulum klasörüne yazılamıyor:$\r$\n$INSTDIR$\r$\n$\r$\nKurulumu yönetici yetkisiyle yeniden başlatın veya kurumunuzun BT yöneticisine başvurun."
  Abort
FunctionEnd

Section "DoğruFatura" SEC_MAIN
  SetShellVarContext all
  SetRegView 64

  ; Kurulum ortamını önceden doğrula.
  Call CheckInstallEnvironment

  ; Güncellemede çalışan eski sürümün dosya kilitlerini güvenli biçimde kaldır.
  Call CloseDogruFatura

  ; Kullanıcı verileri Masaüstü\DoğruFatura altında olduğundan yalnız Program Files içindeki
  ; eski uygulama dosyaları temizlenir. Bu, eski/stale DLL kalmasını da engeller.
  IfFileExists "$INSTDIR\*.*" 0 clean_install_done

  ClearErrors
  RMDir /r "$INSTDIR"
  IfErrors 0 clean_install_done

  MessageBox MB_RETRYCANCEL|MB_ICONEXCLAMATION "DoğruFatura'nın önceki sürümüne ait bazı dosyalar halen Windows tarafından kullanılıyor.$\r$\n$\r$\nAçık DoğruFatura pencerelerini kapatın ve Yeniden Dene düğmesine basın." IDRETRY retry_clean_install IDCANCEL cancel_install

retry_clean_install:
  Call CloseDogruFatura
  ClearErrors
  RMDir /r "$INSTDIR"
  IfErrors 0 clean_install_done

  MessageBox MB_ICONSTOP|MB_OK "Önceki sürümün dosya kilidi kaldırılamadı.$\r$\n$\r$\nBilgisayarı yeniden başlatın ve kurulumu yeniden çalıştırın. Kullanıcı verileriniz korunur."
  Abort

cancel_install:
  Abort

clean_install_done:
  CreateDirectory "$INSTDIR"
  SetOutPath "$INSTDIR"
  SetOverwrite on
  File /r "..\_YAYIN\Uygulama\*.*"

  WriteUninstaller "$INSTDIR\Uninstall.exe"

  CreateDirectory "$SMPROGRAMS\DoğruFatura"
  CreateShortcut "$SMPROGRAMS\DoğruFatura\DoğruFatura.lnk" "$INSTDIR\${APPEXENAME}" "" "$INSTDIR\${APPEXENAME}" 0
  CreateShortcut "$SMPROGRAMS\DoğruFatura\DoğruFatura'yı Kaldır.lnk" "$INSTDIR\Uninstall.exe"
  CreateShortcut "$DESKTOP\DoğruFatura.lnk" "$INSTDIR\${APPEXENAME}" "" "$INSTDIR\${APPEXENAME}" 0

  WriteRegStr HKLM "${APPKEY}" "InstallDir" "$INSTDIR"
  WriteRegStr HKLM "${APPKEY}" "Version" "${APPVERSION}"
  WriteRegStr HKLM "${UNINSTKEY}" "DisplayName" "DoğruFatura ${APPVERSION}"
  WriteRegStr HKLM "${UNINSTKEY}" "DisplayVersion" "${APPVERSION}"
  WriteRegStr HKLM "${UNINSTKEY}" "Publisher" "DoğruFatura"
  WriteRegStr HKLM "${UNINSTKEY}" "DisplayIcon" "$INSTDIR\${APPEXENAME}"
  WriteRegStr HKLM "${UNINSTKEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "${UNINSTKEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegStr HKLM "${UNINSTKEY}" "QuietUninstallString" '"$INSTDIR\Uninstall.exe" /S'
  WriteRegDWORD HKLM "${UNINSTKEY}" "NoModify" 1
  WriteRegDWORD HKLM "${UNINSTKEY}" "NoRepair" 1
SectionEnd

Section "Uninstall"
  SetShellVarContext all
  SetRegView 64

  nsExec::ExecToStack '"$SYSDIR\taskkill.exe" /IM "${APPEXENAME}" /T /F'
  Pop $0
  Pop $1
  Sleep 2500

  Delete "$DESKTOP\DoğruFatura.lnk"
  Delete "$SMPROGRAMS\DoğruFatura\DoğruFatura.lnk"
  Delete "$SMPROGRAMS\DoğruFatura\DoğruFatura'yı Kaldır.lnk"
  RMDir "$SMPROGRAMS\DoğruFatura"

  ; Yalnız uygulama kurulum klasörü silinir.
  ; Masaüstü\DoğruFatura içindeki veri/yedek/loglar ve PDF belgeleri
  ; BİLİNÇLİ olarak korunur.
  RMDir /r "$INSTDIR"

  DeleteRegKey HKLM "${UNINSTKEY}"
  DeleteRegKey HKLM "${APPKEY}"
SectionEnd
