using DogruFatura.Wpf;
using System.IO.Compression;
using System.Text;

namespace DogruFatura.Tests;

public sealed class DiagnosticAndRecoveryTests
{
    [Fact]
    public void DiagnosticPackageContainsSummaryAndLogsButNeverUserJsonOrPdf()
    {
        using var temp = new TempEnvironment();
        var safety = new ApplicationSafetyService(temp.Documents, temp.Desktop, () => "1.0.0-rc1-test");
        safety.InitializeAndCheck();
        Directory.CreateDirectory(safety.DataFolder);
        Directory.CreateDirectory(safety.ActivePdfFolder);
        Directory.CreateDirectory(safety.LogFolder);
        File.WriteAllText(Path.Combine(safety.DataFolder, "Okullar.json"), "[{\"secret\":true}]", Encoding.UTF8);
        File.WriteAllBytes(Path.Combine(safety.ActivePdfFolder, "secret.pdf"), Encoding.ASCII.GetBytes("%PDF-1.4\nsecret"));
        var sensitiveGuid = "2f7e1282-c25d-4d35-9a62-9d3d2ae477e1";
        var sensitiveLog = string.Join(
            Environment.NewLine,
            "C:\\Users\\TestUser\\DoğruFatura hata",
            "Kurum: Atatürk İlkokulu",
            "VKN=1234567890",
            "Hizmet No: 9876543210",
            "Fatura No: ELK-2026-000123",
            "Yetkili: Ayşe Yılmaz",
            "E-posta: ayse@example.com",
            "Kayıt Id: " + sensitiveGuid,
            "İstemci: 192.168.1.24",
            "Veri yolu: " + Path.Combine(safety.DataFolder, "AktifFaturalar.json"),
            new string('X', 100_000) + " Kurum: Çok Uzun Satırdaki Gizli Kurum");
        File.WriteAllText(Path.Combine(safety.LogFolder, "test.log"), sensitiveLog, Encoding.UTF8);

        var package = DiagnosticPackageService.Create(safety);

        Assert.True(File.Exists(package));
        using var zip = ZipFile.OpenRead(package);
        var names = zip.Entries.Select(entry => entry.FullName).ToArray();
        Assert.Contains("Tanilama.txt", names);
        Assert.Contains("Loglar/test.log", names);
        Assert.DoesNotContain(names, name => name.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));

        var logEntry = Assert.Single(zip.Entries, entry => entry.FullName == "Loglar/test.log");
        using var reader = new StreamReader(logEntry.Open(), Encoding.UTF8);
        var sanitizedLog = reader.ReadToEnd();

        Assert.DoesNotContain("Atatürk İlkokulu", sanitizedLog, StringComparison.Ordinal);
        Assert.DoesNotContain("1234567890", sanitizedLog, StringComparison.Ordinal);
        Assert.DoesNotContain("9876543210", sanitizedLog, StringComparison.Ordinal);
        Assert.DoesNotContain("ELK-2026-000123", sanitizedLog, StringComparison.Ordinal);
        Assert.DoesNotContain("Ayşe Yılmaz", sanitizedLog, StringComparison.Ordinal);
        Assert.DoesNotContain("ayse@example.com", sanitizedLog, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(sensitiveGuid, sanitizedLog, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("192.168.1.24", sanitizedLog, StringComparison.Ordinal);
        Assert.DoesNotContain(safety.DataFolder, sanitizedLog, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Çok Uzun Satırdaki Gizli Kurum", sanitizedLog, StringComparison.Ordinal);
        Assert.Contains("[GİZLENDİ]", sanitizedLog, StringComparison.Ordinal);
        Assert.Contains("%EMAIL%", sanitizedLog, StringComparison.Ordinal);
        Assert.Contains("%GUID%", sanitizedLog, StringComparison.Ordinal);
        Assert.Contains("%IP%", sanitizedLog, StringComparison.Ordinal);
    }

    private sealed class TempEnvironment : IDisposable
    {
        public TempEnvironment()
        {
            Root = Path.Combine(Path.GetTempPath(), "DogruFatura_DiagnosticTests_" + Guid.NewGuid().ToString("N"));
            Documents = Path.Combine(Root, "Documents");
            Desktop = Path.Combine(Root, "Desktop");
            Directory.CreateDirectory(Documents);
            Directory.CreateDirectory(Desktop);
        }

        public string Root { get; }
        public string Documents { get; }
        public string Desktop { get; }

        public void Dispose()
        {
            try { if (Directory.Exists(Root)) Directory.Delete(Root, true); } catch { }
        }
    }
}
