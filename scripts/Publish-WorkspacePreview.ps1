param([switch]$SkipTests)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot
try {
    if (-not $SkipTests) {
        & dotnet test DogruFatura.sln -c Release --nologo
        if ($LASTEXITCODE -ne 0) { throw 'Testler başarısız; paket oluşturulmadı.' }
    }
    $packageFolder = Join-Path $projectRoot 'artifacts/DoğruFatura-1.2.0-preview.1'
    & dotnet publish DogruFatura.Wpf/DogruFatura.Wpf.csproj -c Release -r win-x64 --self-contained true -o $packageFolder /p:PublishSingleFile=false /p:DebugType=None /p:DebugSymbols=false
    if ($LASTEXITCODE -ne 0) { throw 'Yayın derlemesi başarısız.' }
    Copy-Item -LiteralPath (Join-Path $projectRoot 'docs/CALISMA_MASASI_1.2.md') -Destination (Join-Path $packageFolder 'KULLANIM.md')
    $archivePath = Join-Path $projectRoot 'artifacts/DoğruFatura-1.2.0-preview.1-win-x64.zip'
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $temporaryArchive = $archivePath + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
    [System.IO.Compression.ZipFile]::CreateFromDirectory($packageFolder, $temporaryArchive, [System.IO.Compression.CompressionLevel]::Fastest, $false)
    Move-Item -LiteralPath $temporaryArchive -Destination $archivePath -Force
    $digest = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
    Set-Content -LiteralPath ($archivePath + '.sha256') -Value ($digest + '  ' + (Split-Path -Leaf $archivePath)) -Encoding utf8
    Write-Output $archivePath
    Write-Output ('SHA-256: ' + $digest)
} finally { Pop-Location }
