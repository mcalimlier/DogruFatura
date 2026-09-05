$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$sourcePath = Join-Path $repoRoot 'DogruFatura.Wpf\MainWindow.xaml.cs'

if (-not (Test-Path $sourcePath)) {
    throw "MainWindow kaynak dosyası bulunamadı: $sourcePath"
}

$source = [System.IO.File]::ReadAllText($sourcePath)

$parts = @(
    [pscustomobject]@{ Name = 'WindowStartup';     Marker = '    private void ApplyTaskbarIcon()' },
    [pscustomobject]@{ Name = 'InvoiceEntry';     Marker = '    private void InstitutionTypeBox_SelectionChanged' },
    [pscustomobject]@{ Name = 'Dashboard';        Marker = '    private void RefreshDashboard()' },
    [pscustomobject]@{ Name = 'Reports';          Marker = '    private void InvalidateFinalReportPreview()' },
    [pscustomobject]@{ Name = 'InvoiceList';      Marker = '    private int FindInvoiceIndex(Guid invoiceId)' },
    [pscustomobject]@{ Name = 'SchoolsOfficials'; Marker = '    private bool FilterSchool(object item)' },
    [pscustomobject]@{ Name = 'Control';          Marker = '    private bool FilterControlIssue(object item)' },
    [pscustomobject]@{ Name = 'ArchiveView';      Marker = '    private bool FilterArchivedList(object item)' },
    [pscustomobject]@{ Name = 'ArchiveDocuments'; Marker = '    private void SynchronizeArchivePdfs_Click' },
    [pscustomobject]@{ Name = 'ArchiveCommands';  Marker = '    private void ArchiveClosure_Click' },
    [pscustomobject]@{ Name = 'Navigation';       Marker = '    private void ShowView(string view)' },
    [pscustomobject]@{ Name = 'Safety';           Marker = '    private SafetyCheckResult RefreshSafetyInformation()' },
    [pscustomobject]@{ Name = 'WindowChrome';     Marker = '    private void Header_MouseLeftButtonDown' }
)

$firstMarker = $parts[0].Marker
if ($source.IndexOf($firstMarker, [StringComparison]::Ordinal) -lt 0) {
    $existing = Get-ChildItem (Join-Path $repoRoot 'DogruFatura.Wpf') -Filter 'MainWindow.*.cs' -ErrorAction SilentlyContinue
    if ($existing.Count -gt 0) {
        Write-Host 'MainWindow daha önce partial dosyalara ayrılmış görünüyor; işlem tekrarlanmadı.'
        exit 0
    }
    throw 'Refaktör başlangıç işareti bulunamadı.'
}

$positions = New-Object System.Collections.Generic.List[int]
foreach ($part in $parts) {
    $index = $source.IndexOf($part.Marker, [StringComparison]::Ordinal)
    if ($index -lt 0) {
        throw "Refaktör işareti bulunamadı: $($part.Marker)"
    }
    $positions.Add($index)
}

for ($i = 1; $i -lt $positions.Count; $i++) {
    if ($positions[$i] -le $positions[$i - 1]) {
        throw "Refaktör işaret sırası geçersiz: $($parts[$i].Name)"
    }
}

$prefix = $source.Substring(0, $positions[0]).TrimEnd()
if (-not $prefix.Contains('public partial class MainWindow : Window')) {
    throw 'MainWindow sınıf bildirimi korunamadı; işlem iptal edildi.'
}
if (-not $prefix.Contains('public MainWindow()')) {
    throw 'MainWindow kurucusu korunamadı; işlem iptal edildi.'
}

$commonUsings = @'
using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace DogruFatura.Wpf;

public partial class MainWindow
{
'@

$generatedPaths = New-Object System.Collections.Generic.List[string]
$capturedLength = 0
for ($i = 0; $i -lt $parts.Count; $i++) {
    $start = $positions[$i]
    $end = if ($i -lt $parts.Count - 1) { $positions[$i + 1] } else { $source.Length }
    $body = $source.Substring($start, $end - $start).TrimEnd()

    if ($i -eq $parts.Count - 1) {
        # Son parçadaki ana sınıfın kapanış süslü parantezini yeni wrapper dışında bırak.
        $body = [regex]::Replace($body, '(?s)\r?\n}\s*$', '')
    }

    if ([string]::IsNullOrWhiteSpace($body)) {
        throw "Boş partial bölüm üretildi: $($parts[$i].Name)"
    }

    $capturedLength += $body.Length
    $targetPath = Join-Path $repoRoot ("DogruFatura.Wpf\MainWindow.{0}.cs" -f $parts[$i].Name)
    $generated = $commonUsings + $body + "`r`n}`r`n"
    [System.IO.File]::WriteAllText($targetPath, $generated, [System.Text.UTF8Encoding]::new($false))
    $generatedPaths.Add($targetPath)
    Write-Host ("Oluşturuldu: {0}" -f (Split-Path -Leaf $targetPath))
}

$reducedMain = $prefix + "`r`n}`r`n"
[System.IO.File]::WriteAllText($sourcePath, $reducedMain, [System.Text.UTF8Encoding]::new($false))

# Yapısal emniyet kontrolleri: kurucu ana dosyada kalmalı; taşınan ana sorumluluklar
# eski monolit dosyada artık bulunmamalı.
$verifyMain = [System.IO.File]::ReadAllText($sourcePath)
foreach ($marker in $parts.Marker) {
    if ($verifyMain.Contains($marker)) {
        throw "Taşınması gereken kod ana dosyada kaldı: $marker"
    }
}
if (-not $verifyMain.Contains('public MainWindow()')) {
    throw 'Kurucu refaktör sonrasında kayboldu.'
}

foreach ($path in $generatedPaths) {
    $text = [System.IO.File]::ReadAllText($path)
    if (-not $text.Contains('public partial class MainWindow')) {
        throw "Geçersiz partial dosya: $path"
    }
}

Write-Host ''
Write-Host 'MainWindow güvenli biçimde görev bazlı partial dosyalara ayrıldı.'
Write-Host ("Ana dosya karakter sayısı: {0:N0}" -f $verifyMain.Length)
Write-Host ("Üretilen partial dosya sayısı: {0}" -f $generatedPaths.Count)
