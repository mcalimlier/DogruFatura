using DogruFatura.Core.Models;
using System.Text.Json;

namespace DogruFatura.Core.Services;

public sealed record InvoiceDraft(Guid Id, string Name, DateTimeOffset SavedAt, List<InvoiceRecord> Invoices)
{
    public override string ToString() => $"{Name} · {Invoices.Count} fatura · {SavedAt:dd.MM HH:mm}";
}

public sealed class InvoiceDraftStore(string filePath)
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public IReadOnlyList<InvoiceDraft> Load()
    {
        if (!File.Exists(filePath)) return Array.Empty<InvoiceDraft>();
        var items = JsonSerializer.Deserialize<List<InvoiceDraft>>(File.ReadAllText(filePath)) ?? throw new InvalidDataException("Taslak dosyası boş.");
        Validate(items);
        return items;
    }

    public void Save(IEnumerable<InvoiceDraft> drafts)
    {
        var items = drafts.ToArray();
        Validate(items);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
        var temp = filePath + ".tmp";
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(items, Options));
            File.Move(temp, filePath, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private static void Validate(IEnumerable<InvoiceDraft> items)
    {
        var ids = new HashSet<Guid>();
        foreach (var draft in items)
        {
            if (draft is null || draft.Id == Guid.Empty || !ids.Add(draft.Id) || string.IsNullOrWhiteSpace(draft.Name) || draft.Invoices is null)
                throw new InvalidDataException("Taslak kaydı geçersiz.");
            foreach (var invoice in draft.Invoices)
                if (invoice is null || !InvoiceValidationService.Validate(invoice).IsValid)
                    throw new InvalidDataException("Taslak içinde geçersiz fatura var.");
            if (draft.Invoices.Select(i => i.Id).Distinct().Count() != draft.Invoices.Count
                || draft.Invoices.Select(i => i.InvoiceType).Distinct().Count() > 1)
                throw new InvalidDataException("Taslak kimlikleri veya fatura türleri tutarsız.");
        }
    }
}
