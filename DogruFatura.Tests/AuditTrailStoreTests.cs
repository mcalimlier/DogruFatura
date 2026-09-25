using DogruFatura.Core.Models;
using DogruFatura.Core.Services;

namespace DogruFatura.Tests;

public sealed class AuditTrailStoreTests
{
    [Fact]
    public void AppendPreservesExistingBytesAndAddsReadableEvent()
    {
        using var temp = new TempFolder();
        var path = Path.Combine(temp.Path, "DenetimKaydi.jsonl");
        var store = new AuditTrailStore(path);

        var first = AuditTrailEntry.Create(
            AuditTrailEventType.InvoiceCreated,
            AuditTrailEntityType.Invoice,
            Guid.NewGuid().ToString("D"),
            "Type=Elektrik;Rule=DV-0.00948-R1");
        store.Append(first);
        var firstBytes = File.ReadAllBytes(path);

        var second = AuditTrailEntry.Create(
            AuditTrailEventType.SchoolUpdated,
            AuditTrailEntityType.School,
            Guid.NewGuid().ToString("D"),
            "Kind=PrimarySchool");
        store.Append(second);

        var allBytes = File.ReadAllBytes(path);
        Assert.True(allBytes.Length > firstBytes.Length);
        Assert.True(allBytes.AsSpan(0, firstBytes.Length).SequenceEqual(firstBytes));

        var entries = store.ReadAll();
        Assert.Equal(2, entries.Count);
        Assert.Equal(first.EventId, entries[0].EventId);
        Assert.Equal(AuditTrailEventType.InvoiceCreated, entries[0].EventType);
        Assert.Equal(second.EventId, entries[1].EventId);
        Assert.Equal(AuditTrailEventType.SchoolUpdated, entries[1].EventType);
        Assert.All(entries, item => Assert.Equal(TimeSpan.Zero, item.OccurredAtUtc.Offset));
    }

    private sealed class TempFolder : IDisposable
    {
        public TempFolder()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "DogruFatura_AuditTrailTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                    Directory.Delete(Path, true);
            }
            catch
            {
            }
        }
    }
}
