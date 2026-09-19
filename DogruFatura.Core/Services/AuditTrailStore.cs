using DogruFatura.Core.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DogruFatura.Core.Services;

public sealed class AuditTrailStore
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public AuditTrailStore(string? filePath = null)
    {
        FilePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "DoğruFatura",
            "Veri",
            "DenetimKaydi.jsonl");
    }

    public string FilePath { get; }

    public void Append(AuditTrailEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.EventId == Guid.Empty)
            throw new ArgumentException("Denetim olay kimliği boş olamaz.", nameof(entry));
        if (string.IsNullOrWhiteSpace(entry.EntityId))
            throw new ArgumentException("Denetim entity kimliği boş olamaz.", nameof(entry));

        var folder = Path.GetDirectoryName(FilePath)
                     ?? throw new InvalidOperationException("Denetim kayıt klasörü belirlenemedi.");
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

    public IReadOnlyList<AuditTrailEntry> ReadAll()
    {
        if (!File.Exists(FilePath))
            return Array.Empty<AuditTrailEntry>();

        var result = new List<AuditTrailEntry>();
        foreach (var line in File.ReadLines(FilePath, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var entry = JsonSerializer.Deserialize<AuditTrailEntry>(line, _jsonOptions)
                        ?? throw new JsonException("Denetim kaydı satırı okunamadı.");
            result.Add(entry);
        }

        return result;
    }
}
