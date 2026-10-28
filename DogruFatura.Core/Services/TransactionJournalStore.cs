using DogruFatura.Core.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DogruFatura.Core.Services;

public sealed class TransactionJournalStore
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public TransactionJournalStore(string? filePath = null)
    {
        FilePath = filePath ?? Path.Combine(
            RuntimeInstallationProfile.RootDirectory,
            "Veri",
            "IslemJournal.jsonl");
    }

    public string FilePath { get; }

    public void Append(TransactionJournalEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.EntryId == Guid.Empty)
            throw new ArgumentException("Journal olay kimliği boş olamaz.", nameof(entry));
        if (entry.TransactionId == Guid.Empty)
            throw new ArgumentException("Journal işlem kimliği boş olamaz.", nameof(entry));
        if (string.IsNullOrWhiteSpace(entry.EntityId))
            throw new ArgumentException("Journal entity kimliği boş olamaz.", nameof(entry));

        var folder = Path.GetDirectoryName(FilePath)
                     ?? throw new InvalidOperationException("İşlem journal klasörü belirlenemedi.");
        Directory.CreateDirectory(folder);

        var json = JsonSerializer.Serialize(entry, _jsonOptions);
        using var stream = new FileStream(
            FilePath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.Read,
            4096,
            FileOptions.WriteThrough);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, leaveOpen: true);
        writer.WriteLine(json);
        writer.Flush();
        stream.Flush(flushToDisk: true);
    }

    public IReadOnlyList<TransactionJournalEntry> ReadAll()
    {
        if (!File.Exists(FilePath))
            return Array.Empty<TransactionJournalEntry>();

        var lines = File.ReadAllLines(FilePath, Encoding.UTF8);
        var result = new List<TransactionJournalEntry>(lines.Length);
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var entry = JsonSerializer.Deserialize<TransactionJournalEntry>(line, _jsonOptions)
                            ?? throw new JsonException("İşlem journal satırı okunamadı.");
                result.Add(entry);
            }
            catch (JsonException) when (index == lines.Length - 1)
            {
                // Ani kapanmada yalnız son append satırı yarım kalmış olabilir.
                // Önceki sağlam journal olayları korunur ve incomplete transaction
                // tespiti bu sağlam prefix üzerinden devam eder.
                break;
            }
        }

        return result;
    }

    public IReadOnlyList<TransactionJournalEntry> GetIncompleteTransactions() =>
        ReadAll()
            .GroupBy(entry => entry.TransactionId)
            .Select(group => group.Last())
            .Where(entry => !entry.IsTerminal)
            .ToArray();
}
