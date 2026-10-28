using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Text;
using System.Text.Json;

if (args.Length < 2) throw new ArgumentException("Usage: ArchiveMaintenance data-folder report-path [--apply]");
if (System.Diagnostics.Process.GetProcessesByName("DogruFatura").Length > 0) throw new InvalidOperationException("Önce DoğruFatura kapatılmalı.");
var folder = Path.GetFullPath(args[0]);
var reportPath = Path.GetFullPath(args[1]);
var archivePath = Path.Combine(folder, "IslemArsivi.json");
var schoolsPath = Path.Combine(folder, "Okullar.json");
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
List<T> Read<T>(string path, string kind) => JsonSerializer.Deserialize<List<T>>(DataFileSchemaService.Read(File.ReadAllText(path), kind).Data.GetRawText(), options) ?? throw new InvalidDataException(path);
var schools = Read<SchoolRecord>(schoolsPath, DataFileKinds.Schools).Select(SchoolValidationService.Normalize).ToArray();
var original = Read<ArchivedInvoiceList>(archivePath, DataFileKinds.InvoiceArchive).ToArray();
foreach (var a in original) { var v = ArchivedInvoiceListValidationService.Validate(a); if (!v.IsValid) throw new InvalidDataException(a.ArchiveNumber + ": " + v.Message); }
var duplicateNames = schools.GroupBy(s => s.Name.Trim(), StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"), true)).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
if (duplicateNames.Length > 0) throw new InvalidDataException("Mükerrer okul adları ayrıca incelenmeli: " + string.Join(", ", duplicateNames));
var changes = new List<object>();
var unmatched = new SortedSet<string>();
var changedIds = new List<Guid>();
var updated = original.Select(a =>
{
    var changed = false;
    var invoices = a.Invoices.Select(i =>
    {
        var resolved = InstitutionResolutionService.ForReporting(i, schools);
        if (resolved.InstitutionName == i.InstitutionName)
        {
            if (!schools.Any(s => s.Name == i.InstitutionName)) unmatched.Add(i.InstitutionName);
            return i;
        }
        changes.Add(new { Archive = a.ArchiveNumber, Invoice = i.Id, Before = i.InstitutionName, After = resolved.InstitutionName });
        changed = true; return resolved;
    }).ToList();
    var evidence = (a.LegacyEvidenceItems ?? new()).Select(e =>
    {
        var school = InstitutionResolutionService.Resolve(e.InstitutionName, a.InvoiceType, e.ServiceNumber, Guid.Empty, schools);
        if (school is null) { unmatched.Add(e.InstitutionName); return e; }
        if (school.Name == e.InstitutionName) return e;
        changes.Add(new { Archive = a.ArchiveNumber, Invoice = e.InvoiceNumber, Before = e.InstitutionName, After = school.Name });
        changed = true; return e with { InstitutionName = school.Name };
    }).ToList();
    if (!changed) return a;
    var result = a with { Invoices = invoices, LegacyEvidenceItems = evidence };
    if (result.IsClosed) result = ClosedArchiveSnapshotService.Seal(result);
    var validation = ArchivedInvoiceListValidationService.Validate(result);
    if (!validation.IsValid) throw new InvalidDataException(validation.Message);
    changedIds.Add(a.Id); return result;
}).ToArray();
var summary = new { Applied = args.Contains("--apply"), Schools = schools.Length, DuplicateSchoolNames = duplicateNames, Archives = original.Length, Invoices = original.Sum(a => a.InvoiceCount), ChangedArchives = changedIds.Count, CorrectedNames = changes.Count, PaymentBefore = original.Sum(a => a.PaymentTotal), PaymentAfter = updated.Sum(a => a.PaymentTotal), UnmatchedNames = unmatched, Changes = changes };
Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
File.WriteAllText(reportPath, JsonSerializer.Serialize(summary, options), new UTF8Encoding(false));
Console.WriteLine(JsonSerializer.Serialize(new { summary.Applied, summary.Schools, summary.Archives, summary.Invoices, summary.ChangedArchives, summary.CorrectedNames, summary.PaymentBefore, summary.PaymentAfter, UnmatchedNames = unmatched }));
if (!args.Contains("--apply")) return;
if (summary.PaymentBefore != summary.PaymentAfter) throw new InvalidOperationException("Toplam değişti.");
var backupPath = archivePath + ".institution-repair-temporary-backup";
if (File.Exists(backupPath)) throw new IOException("Önceki geçici yedek incelenmeli.");
File.Copy(archivePath, backupPath, false);
try
{
    new ArchivedInvoiceListDataStore(archivePath).Save(updated);
    var loaded = new ArchivedInvoiceListDataStore(archivePath).LoadOrCreate();
    if (loaded.WarningMessage is not null || loaded.Archives.Count != original.Length || loaded.Archives.Sum(a => a.InvoiceCount) != summary.Invoices || loaded.Archives.Sum(a => a.PaymentTotal) != summary.PaymentBefore) throw new InvalidDataException("Yeniden okuma kontrolü başarısız.");
    foreach (var a in loaded.Archives.Where(a => a.IsClosed)) if (!ClosedArchiveSnapshotService.Matches(a)) throw new InvalidDataException("Mühür doğrulanamadı.");
    var audit = new AuditTrailStore(Path.Combine(folder, "DenetimKaydi.jsonl"));
    foreach (var id in changedIds) audit.Append(AuditTrailEntry.Create(AuditTrailEventType.ArchiveInstitutionNamesCorrected, AuditTrailEntityType.Archive, id.ToString("D"), "Kullanıcı isteğiyle PDF sıra numarası ve kurum adı eşleştirmesi düzeltildi. Fatura/HYS/tarih/tutar/PDF korundu."));
}
catch { File.Copy(backupPath, archivePath, true); throw; }
// User explicitly requested removal of this temporary backup after successful validation.
File.Delete(backupPath);
Console.WriteLine("VERIFIED: archive persisted, all seals/totals/counts valid; temporary backup removed.");
