param([string]$OutputFolder = '')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (!$OutputFolder) { $OutputFolder = Join-Path $projectRoot 'DAGITIM/1.2.0-preview.1' }
$manifest = Get-Content -LiteralPath (Join-Path $OutputFolder 'PAKETLER.json') -Raw | ConvertFrom-Json
$verificationRoot = Join-Path $projectRoot ('artifacts/install-verification-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $verificationRoot | Out-Null
$results = @()
foreach ($entry in $manifest) {
    $setup = Join-Path $OutputFolder $entry.File
    if ((Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash -ne $entry.SHA256) { throw 'Kurulum hash hatalı.' }
    $description = (Get-Item -LiteralPath $setup).VersionInfo.FileDescription
    if ($description -ne ('DoğruFatura - ' + $entry.Title)) { throw 'Kurulum başlığı paket türüyle eşleşmiyor.' }
    $target = Join-Path $verificationRoot ([IO.Path]::GetFileNameWithoutExtension($entry.File))
    $install = Start-Process -FilePath $setup -ArgumentList "/S /VERIFYONLY /D=$target" -WindowStyle Hidden -PassThru -Wait
    if ($install.ExitCode -ne 0) { throw "Kurulum doğrulaması başarısız: $($install.ExitCode)" }
    $profileCode = if ($entry.Personal) { '1' } else { '0' }
    $registrationPath = 'HKCU:/Software/DoğruFatura/InstallerVerification/' + $profileCode
    $registration = Get-ItemProperty -LiteralPath $registrationPath
    $appExe = Join-Path $target 'DogruFatura.exe'
    if ($registration.InstallLocation -ne $target -or $registration.DisplayIcon -ne ('"' + $appExe + '",0')) { throw 'Denetim Masası simge kaydı hatalı.' }
    $desktopLink = Get-ChildItem -LiteralPath (Join-Path $target 'Verification/Desktop') -File -Filter '*.lnk' | Select-Object -First 1
    $shortcut = (New-Object -ComObject WScript.Shell).CreateShortcut($desktopLink.FullName)
    if ($shortcut.TargetPath -ne $appExe -or !$shortcut.IconLocation.EndsWith(',0') -or !$shortcut.IconLocation.Contains($appExe)) { throw 'Masaüstü kısayolu veya simgesi hatalı.' }
    Add-Type -AssemblyName System.Drawing
    $icon = [Drawing.Icon]::ExtractAssociatedIcon($appExe)
    if (!$icon -or $icon.Width -eq 0) { throw 'Program simgesi okunamadı.' }
    $icon.Dispose()
    Remove-Item -LiteralPath $registrationPath
    $core = Join-Path $target 'DogruFatura.Core.dll'
    foreach ($required in 'DogruFatura.exe','coreclr.dll','PresentationFramework.dll') { if (!(Test-Path (Join-Path $target $required))) { throw "Kurulum dosyası eksik: $required" } }
    $seedRoot = [IO.Path]::GetFullPath((Join-Path $target 'InitialData'))
    foreach ($name in 'IslemArsivi.json','AktifFaturalar.json','YetkiliProfili.json','DenetimKaydi.jsonl','IslemJournal.jsonl') {
        foreach ($record in Get-ChildItem -LiteralPath $target -Recurse -File -Filter $name) {
            if (!$entry.Personal -or !$record.FullName.StartsWith($seedRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw "Paket özel veri içeriyor: $name" }
        }
    }
    if (!$entry.Personal -and (Get-ChildItem -LiteralPath $target -Recurse -File -Include '*.pdf','*.zip')) { throw 'PDF/yedek dağıtım paketine karışmış.' }
    if (!$entry.Personal -and (Test-Path -LiteralPath $seedRoot)) { throw 'Dağıtımda kişisel veri klasörü var.' }
    if ($entry.Personal -and !(Test-Path -LiteralPath (Join-Path $seedRoot 'Veri/IslemArsivi.json'))) { throw 'Kişisel kurulumda tam veri paketi yok.' }
    $binaryHex = [Convert]::ToHexString([IO.File]::ReadAllBytes($core))
    foreach ($privateDefault in 'Halil ÖZENÇ','Özgür BAYSAL','7710327776','Cumhuriyet İlkokulu') {
        foreach ($encoding in @([Text.Encoding]::Unicode, [Text.Encoding]::UTF8)) {
            if ($binaryHex.Contains([Convert]::ToHexString($encoding.GetBytes($privateDefault)))) { throw 'Ortak program çekirdeğinde özel başlangıç bilgisi kaldı.' }
        }
    }
    & dotnet build (Join-Path $projectRoot 'scripts/VerifyInstallation/VerifyInstallation.csproj') -c Release -o $target "-p:PackageCore=$core" --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Doğrulama aracı derlenemedi.' }
    $firstRunFolder = Join-Path $target 'IsolatedFirstRun'
    $result = & dotnet (Join-Path $target 'VerifyInstallation.dll') $firstRunFolder $entry.Schools $entry.Archives $entry.Invoices $entry.PrivateSignatures $entry.Personal
    if ($LASTEXITCODE -ne 0) { throw 'İlk açılış kontrolü başarısız.' }
    $results += [pscustomobject]@{File=$entry.File;SetupTitle=$description;DesktopIcon='PASS';UninstallIcon='PASS';FirstRun=($result | ConvertFrom-Json);VerifiedFolder=$target}
}
$results | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $OutputFolder 'DOGRULAMA.json') -Encoding utf8
$results | ConvertTo-Json -Depth 5
