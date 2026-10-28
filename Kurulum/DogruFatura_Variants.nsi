Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "x64.nsh"
!include "WinVer.nsh"
!include "FileFunc.nsh"

!if "${PERSONAL}" == "1"
!define SHORTCUTNAME "DoğruFatura - Kişisel"
!define MUTEXNAME "Local\DogruFatura.Desktop.SingleInstance.2D22EB36-5AA7-4AD9-9814-DB03D1DFBFD3"
!define UNINSTKEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\DogruFatura.Kisisel"
InstallDir "$LOCALAPPDATA\Programs\DogruFatura-Kisisel"
!else
!define SHORTCUTNAME "DoğruFatura"
!define MUTEXNAME "Local\DogruFatura.Desktop.SingleInstance.2D22EB36-5AA7-4AD9-9814-DB03D1DFBFD3.Distribution"
!define UNINSTKEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\DogruFatura.Dagitim"
InstallDir "$LOCALAPPDATA\Programs\DogruFatura"
!endif
Name "DoğruFatura"
OutFile "${OUTPUTEXE}"
RequestExecutionLevel user
CRCCheck force
SetCompressor zlib
ShowInstDetails nevershow
BrandingText "DoğruFatura • Güvenli ve Düzenli Çalışma"
SetFont "Segoe UI" 9
VIProductVersion "1.2.0.0"
VIAddVersionKey /LANG=1055 "ProductName" "DoğruFatura — ${VARIANTTITLE}"
VIAddVersionKey /LANG=1055 "FileDescription" "DoğruFatura - ${VARIANTTITLE}"
VIAddVersionKey /LANG=1055 "FileVersion" "1.2.0.0"
VIAddVersionKey /LANG=1055 "ProductVersion" "${VERSION}"
VIAddVersionKey /LANG=1055 "CompanyName" "DoğruFatura"
VIAddVersionKey /LANG=1055 "LegalCopyright" "DoğruFatura"
!define MUI_ICON "${APPICON}"
!define MUI_UNICON "${APPICON}"
!define MUI_BGCOLOR "F6F8FC"
!define MUI_TEXTCOLOR "10283F"
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_RIGHT
!define MUI_HEADERIMAGE_BITMAP "${ARTWORK}\header.bmp"
!define MUI_WELCOMEFINISHPAGE_BITMAP "${ARTWORK}\wizard.bmp"
!define MUI_WELCOMEPAGE_TITLE "DoğruFatura'ya$\r$\nHoş Geldiniz"
!define MUI_WELCOMEPAGE_TEXT "Faturalarınızı düzenleyin, ödeme listelerinizi hazırlayın ve arşivinizi yönetin.$\r$\n$\r$\n${VARIANTSHORT}$\r$\n${VARIANTBODY}$\r$\n$\r$\nWindows 10/11 x64. Excel ve .NET kurulumu gerekmez.$\r$\n$\r$\nDevam etmek için İleri'yi seçin."
!define MUI_DIRECTORYPAGE_TEXT_TOP "Program dosyalarının kurulacağı klasörü seçin. Kullanıcı kayıtlarınız bu klasörden ayrı tutulur."
!define MUI_FINISHPAGE_TITLE "DoğruFatura Hazır"
!define MUI_FINISHPAGE_TEXT "Kurulum başarıyla tamamlandı.$\r$\n$\r$\n${VARIANTSHORT}$\r$\n$\r$\nKayıtlarınız program dosyalarından ayrı tutulur. Programı kaldırmak kayıtlarınızı silmez."
!define MUI_FINISHPAGE_RUN "$INSTDIR\DogruFatura.exe"
!define MUI_FINISHPAGE_RUN_TEXT "DoğruFatura'yı Çalıştır"
!insertmacro MUI_PAGE_WELCOME
!define MUI_PAGE_HEADER_TEXT "Kurulum Konumu"
!define MUI_PAGE_HEADER_SUBTEXT "Program dosyalarının kaydedileceği klasörü seçin."
!insertmacro MUI_PAGE_DIRECTORY
!define MUI_PAGE_HEADER_TEXT "DoğruFatura Hazırlanıyor"
!define MUI_PAGE_HEADER_SUBTEXT "Dosyalar yüklenirken lütfen bekleyin."
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "Turkish"
Var VerifyOnly
Var RegistrationKey
Var DesktopLinkRoot
Var MenuLinkRoot

Function .onInit
  ${IfNot} ${RunningX64}
    Abort "64-bit Windows gerekir."
  ${EndIf}
  ${IfNot} ${AtLeastWin10}
    Abort "Windows 10 veya daha yeni bir sürüm gerekir."
  ${EndIf}
  ${GetParameters} $R0
  ClearErrors
  ${GetOptions} $R0 "/VERIFYONLY" $R1
  ${IfNot} ${Errors}
    StrCpy $VerifyOnly "1"
  ${EndIf}
  System::Call 'kernel32::OpenMutexW(i 1048576, i 0, w "${MUTEXNAME}") p.r0'
  ${If} $0 != 0
    System::Call 'kernel32::CloseHandle(p r0)'
    MessageBox MB_OK|MB_ICONEXCLAMATION "Önce açık DoğruFatura penceresini kapatın. Verilerinizde değişiklik yapılmadı." /SD IDOK
    Abort
  ${EndIf}
FunctionEnd

Section "DoğruFatura"
  SetOutPath "$INSTDIR"
  SetOverwrite on
  File /r "${PAYLOAD}\*.*"
  ; The shared binary has no embedded private defaults. Only this folder differs.
  RMDir /r "$INSTDIR\InitialData"
!if "${PERSONAL}" == "1"
  SetOutPath "$INSTDIR\InitialData"
  File /r "${SEEDROOT}\*.*"
!else
  ; Remove only the previous install's optional seed, never the user's school store.
  RMDir /r "$INSTDIR\InitialData"
!endif
!if "${PERSONAL}" == "1"
  Delete "$INSTDIR\distribution.profile"
!else
  FileOpen $0 "$INSTDIR\distribution.profile" w
  FileWrite $0 "distribution"
  FileClose $0
!endif
  ${If} $VerifyOnly == "1"
    StrCpy $RegistrationKey "Software\DoğruFatura\InstallerVerification\${PERSONAL}"
    StrCpy $DesktopLinkRoot "$INSTDIR\Verification\Desktop"
    StrCpy $MenuLinkRoot "$INSTDIR\Verification\Menu"
    CreateDirectory $DesktopLinkRoot
  ${Else}
    StrCpy $RegistrationKey "${UNINSTKEY}"
    StrCpy $DesktopLinkRoot "$DESKTOP"
    StrCpy $MenuLinkRoot "$SMPROGRAMS\${SHORTCUTNAME}"
    WriteUninstaller "$INSTDIR\Uninstall.exe"
    ReadRegStr $R0 HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DogruFatura" "InstallLocation"
    ${If} $R0 == $INSTDIR
      DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DogruFatura"
    ${EndIf}
  ${EndIf}
  CreateDirectory $MenuLinkRoot
  CreateShortcut "$MenuLinkRoot\${SHORTCUTNAME}.lnk" "$INSTDIR\DogruFatura.exe" "" "$INSTDIR\DogruFatura.exe" 0
  CreateShortcut "$DesktopLinkRoot\${SHORTCUTNAME}.lnk" "$INSTDIR\DogruFatura.exe" "" "$INSTDIR\DogruFatura.exe" 0
  WriteRegStr HKCU $RegistrationKey "DisplayName" "DoğruFatura — ${VARIANTTITLE}"
  WriteRegStr HKCU $RegistrationKey "DisplayVersion" "${VERSION}"
  WriteRegStr HKCU $RegistrationKey "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegStr HKCU $RegistrationKey "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU $RegistrationKey "DisplayIcon" '"$INSTDIR\DogruFatura.exe",0'
  WriteRegStr HKCU $RegistrationKey "Publisher" "DoğruFatura"
  WriteRegDWORD HKCU $RegistrationKey "NoModify" 1
  WriteRegDWORD HKCU $RegistrationKey "NoRepair" 1
SectionEnd

Section "Uninstall"
  Delete "$DESKTOP\${SHORTCUTNAME}.lnk"
  Delete "$SMPROGRAMS\${SHORTCUTNAME}\${SHORTCUTNAME}.lnk"
  RMDir "$SMPROGRAMS\${SHORTCUTNAME}"
  ; Only this install's application files; desktop user data is deliberately untouched.
  Delete "$INSTDIR\*.dll"
  Delete "$INSTDIR\*.exe"
  Delete "$INSTDIR\*.json"
  Delete "$INSTDIR\distribution.profile"
  RMDir /r "$INSTDIR\InitialData"
  RMDir /r "$INSTDIR\tessdata"
  RMDir /r "$INSTDIR\runtimes"
  RMDir "$INSTDIR"
  DeleteRegKey HKCU "${UNINSTKEY}"
SectionEnd
