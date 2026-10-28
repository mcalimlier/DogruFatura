using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Security.Cryptography;
using System.Text.Json;

var root = Path.GetFullPath(args[0]);
System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.CurrentCulture;
var selfTest = EngineSelfTest.Run();
if (!selfTest.Success) throw new InvalidDataException("Kurulu programın öz testi başarısız: " + selfTest.Message);
var count = int.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);
var expectedArchives = int.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture);
var expectedActive = int.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture);
var expectedSignatures = bool.Parse(args[4]);
var personal = bool.Parse(args[5]);
if (RuntimeInstallationProfile.IsDistribution == personal) throw new InvalidDataException("Kurulum veri profili işareti yanlış.");
var expectedDefaultRoot = RuntimeInstallationProfile.RootDirectory;
foreach (var file in new[] { new SchoolDataStore().DataFilePath, new AuthorityProfileDataStore().DataFilePath, new InvoiceDataStore().DataFilePath, new ArchivedInvoiceListDataStore().DataFilePath, new AuditTrailStore().FilePath, new TransactionJournalStore().FilePath })
    if (!file.StartsWith(expectedDefaultRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Veri deposu başka profilin klasörünü kullanıyor.");
if (new DocumentArchiveStorageService().RootDirectory != expectedDefaultRoot) throw new InvalidDataException("PDF veri profili yanlış.");
var initialized = InitialUserDataService.Initialize(Path.Combine(AppContext.BaseDirectory, "InitialData"), root);
if (initialized != personal) throw new InvalidDataException("İlk açılış veri aktarımı paket türüne uymuyor.");
if (personal)
{
    var seedRoot = Path.Combine(AppContext.BaseDirectory, "InitialData");
    var expectedHashes = JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(Path.Combine(seedRoot, "Manifest.json")))!;
    foreach (var entry in expectedHashes)
        if (!FileIntegrityService.MatchesSha256(Path.Combine(root, entry.Key), entry.Value)) throw new InvalidDataException("Tam veri aktarımı eksik.");
}
var dataRoot = Path.Combine(root, "Veri"); Directory.CreateDirectory(dataRoot);
var schools = new SchoolDataStore(Path.Combine(dataRoot, "Okullar.json"));
var first = schools.LoadOrCreate();
if (first.Schools.Count != count || first.WarningMessage is not null) throw new InvalidDataException("Başlangıç okul listesi hatalı: " + first.Schools.Count);
if (first.Schools.Select(s => s.Name).Distinct(StringComparer.CurrentCultureIgnoreCase).Count() != count) throw new InvalidDataException("Mükerrer okul var.");
var invoices = new InvoiceDataStore(Path.Combine(dataRoot, "AktifFaturalar.json"));
if (invoices.LoadOrCreate().Invoices.Count != expectedActive) throw new InvalidDataException("Aktif fatura sayısı yanlış.");
var archives = new ArchivedInvoiceListDataStore(Path.Combine(dataRoot, "IslemArsivi.json"));
var archiveResult = archives.LoadOrCreate();
if (archiveResult.WarningMessage is not null || archiveResult.Archives.Count != expectedArchives) throw new InvalidDataException("Arşiv sayısı yanlış.");
var authorities = new AuthorityProfileDataStore(Path.Combine(dataRoot, "YetkiliProfili.json"));
var profile = authorities.LoadOrCreate().Profile;
if (profile.IsComplete != expectedSignatures || (!expectedSignatures && !profile.IsEmpty)) throw new InvalidDataException("İmza bilgileri paket türüne uymuyor.");
if (authorities.LoadOrCreate().WarningMessage is not null) throw new InvalidDataException("Boş başlangıç imzası yeniden açılamadı.");
DataDirectoryValidationService.ValidateFile(authorities.DataFilePath);
DataDirectoryValidationService.ValidateCompleteDirectory(dataRoot);
var notice = InitialSetupNotice.GetMessage(first.Schools.Count, profile.IsComplete);
if ((notice is not null) == personal) throw new InvalidDataException("İlk kullanım bildirimi paket türüne uymuyor.");
var custom = SchoolValidationService.Normalize(new SchoolRecord("Kurum", "Kullanıcıya Ait Yeni Kurum", "", "", "", "", ""));
schools.Save(first.Schools.Append(custom));
var before = SHA256.HashData(File.ReadAllBytes(schools.DataFilePath));
var savedHashes = Directory.GetFiles(root, "*", SearchOption.AllDirectories).ToDictionary(p => p, FileIntegrityService.ComputeSha256);
if (InitialUserDataService.Initialize(Path.Combine(AppContext.BaseDirectory, "InitialData"), root)) throw new InvalidDataException("Mevcut verilerin üzerine başlangıç paketi uygulandı.");
foreach (var entry in savedHashes) if (!FileIntegrityService.MatchesSha256(entry.Key, entry.Value)) throw new InvalidDataException("Mevcut kullanıcı verisi değişti.");
var second = new SchoolDataStore(schools.DataFilePath).LoadOrCreate();
if (second.Schools.Count != count + 1 || !before.SequenceEqual(SHA256.HashData(File.ReadAllBytes(schools.DataFilePath)))) throw new InvalidDataException("Mevcut veri değişti.");
Console.WriteLine(JsonSerializer.Serialize(new { Result = "PASS", EngineSelfTest = "PASS", DistributionProfile = RuntimeInstallationProfile.IsDistribution, InitialSchools = count, InitialInvoices = expectedActive, InitialArchives = expectedArchives, PrivateSignatures = expectedSignatures, FirstUseNotice = notice is not null, AllUserDataPreserved = true }));
