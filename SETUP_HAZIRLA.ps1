$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

function Fail([string]$Message) { Write-Host "HATA: $Message" -ForegroundColor Red; exit 1 }

[xml]$props = Get-Content (Join-Path $PSScriptRoot 'Directory.Build.props') -Raw -Encoding UTF8
$group = $props.Project.PropertyGroup | Select-Object -First 1
$version = [string]$group.VersionPrefix
$suffix = [string]$group.VersionSuffix
$assemblyVersion = [string]$group.AssemblyVersion
if ([string]::IsNullOrWhiteSpace($version)) { Fail 'VersionPrefix okunamadı.' }
if (-not [string]::IsNullOrWhiteSpace($suffix)) {
    Write-Host "DoğruFatura $version-$suffix bir RC/test sürümüdür." -ForegroundColor Yellow
    Write-Host 'Final Setup güvenlik amacıyla üretilmedi. RC testleri bitince VersionSuffix boşaltılmalıdır.'
    exit 2
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) { Fail '.NET SDK bulunamadı.' }
if ([int]((& dotnet --version).Split('.')[0]) -lt 8) { Fail '.NET 8 veya daha yeni SDK gerekir.' }

$makensis = @(
    "${env:ProgramFiles(x86)}\NSIS\makensis.exe",
    "$env:ProgramFiles\NSIS\makensis.exe",
    "$env:LOCALAPPDATA\Programs\NSIS\makensis.exe"
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $makensis) { $cmd = Get-Command makensis.exe -ErrorAction SilentlyContinue; if ($cmd) { $makensis = $cmd.Source } }
if (-not $makensis) { Fail 'NSIS makensis.exe bulunamadı.' }

$publish = Join-Path $PSScriptRoot '_YAYIN'
$appOut = Join-Path $publish 'Uygulama'
$setupOut = Join-Path $publish 'Setup'
$dist = Join-Path $PSScriptRoot 'DAGITIM'
if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
New-Item -ItemType Directory -Force -Path $appOut,$setupOut,$dist | Out-Null

$project = Join-Path $PSScriptRoot 'DogruFatura.Wpf\DogruFatura.Wpf.csproj'
& dotnet restore $project -r win-x64
if ($LASTEXITCODE -ne 0) { Fail 'Restore başarısız.' }
& dotnet publish $project -c Release -r win-x64 --self-contained true --no-restore -o $appOut /p:PublishSingleFile=false /p:PublishReadyToRun=false /p:DebugType=None /p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { Fail 'Publish başarısız.' }
foreach ($file in 'DogruFatura.exe','coreclr.dll','hostfxr.dll','PresentationFramework.dll') {
    if (-not (Test-Path (Join-Path $appOut $file))) { Fail "Self-contained yayın eksik: $file" }
}

$template = Join-Path $PSScriptRoot 'Kurulum\DogruFatura_NSIS.nsi'
$nsi = Get-Content $template -Raw -Encoding UTF8
$nsi = $nsi.Replace('!define APPVERSION "1.0.0"', "!define APPVERSION `"$version`"")
$nsi = $nsi.Replace('OutFile "..\_YAYIN\Setup\DogruFatura_Setup_1.0.0_x64.exe"', "OutFile `"..\_YAYIN\Setup\DogruFatura_Setup_${version}_x64.exe`"")
$nsi = $nsi.Replace('VIProductVersion "1.0.0.0"', "VIProductVersion `"$assemblyVersion`"")
$nsi = $nsi.Replace('VIAddVersionKey /LANG=1055 "FileVersion" "1.0.0.0"', "VIAddVersionKey /LANG=1055 `"FileVersion`" `"$assemblyVersion`"")
$nsi = $nsi.Replace('VIAddVersionKey /LANG=1055 "ProductVersion" "1.0.0"', "VIAddVersionKey /LANG=1055 `"ProductVersion`" `"$version`"")
$tempNsi = Join-Path $publish 'DogruFatura_NSIS.generated.nsi'
[IO.File]::WriteAllText($tempNsi, $nsi, (New-Object System.Text.UTF8Encoding($true)))

& $makensis /V4 $tempNsi
if ($LASTEXITCODE -ne 0) { Fail 'NSIS Setup oluşturamadı.' }
$setupExe = Join-Path $setupOut "DogruFatura_Setup_${version}_x64.exe"
if (-not (Test-Path $setupExe)) { Fail 'Setup EXE oluşmadı.' }
if ((Get-Item $setupExe).Length -lt 10000000) { Fail 'Setup beklenenden küçük.' }

$finalExe = Join-Path $dist "DogruFatura_Setup_${version}_x64.exe"
Move-Item $setupExe $finalExe -Force
if (Test-Path 'KURULUM_NOTU.txt') { Copy-Item 'KURULUM_NOTU.txt' (Join-Path $dist 'KURULUM_NOTU.txt') -Force }
$hash = (Get-FileHash $finalExe -Algorithm SHA256).Hash
@('DogruFatura Setup SHA-256','',"Dosya : $(Split-Path $finalExe -Leaf)","SHA256: $hash") |
    Set-Content (Join-Path $dist "DogruFatura_Setup_${version}_x64_SHA256.txt") -Encoding UTF8
Remove-Item $publish -Recurse -Force
Write-Host "BAŞARILI: $finalExe" -ForegroundColor Green
