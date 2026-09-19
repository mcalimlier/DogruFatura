using DogruFatura.Core.Models;
using DogruFatura.Core.Services;

namespace DogruFatura.Tests;

public sealed class ClosedArchiveSnapshotTests
{
    [Fact]
    public void SealDetectsUnresealedChangesAndAllowsAuthorizedResealOrDelete()
    {
        using var temp = new TempFolder();
        var path = Path.Combine(temp.Path, "IslemArsivi.json");
        var store = new ArchivedInvoiceListDataStore(path);
        var sealedArchive = ClosedArchiveSnapshotService.Seal(ClosedArchive());

        store.Save(new[] { sealedArchive });

        var changedProfile = sealedArchive.AuthorityProfile with
        {
            RealizationOfficerTitle = "Değiştirilmiş Unvan"
        };
        var changedWithoutReseal = sealedArchive with
        {
            AuthorityProfile = changedProfile
        };

        var mismatchError = Assert.Throws<InvalidOperationException>(
            () => store.Save(new[] { changedWithoutReseal }));
        Assert.Contains("mühür", mismatchError.Message, StringComparison.CurrentCultureIgnoreCase);

        var resealedChangedArchive = ClosedArchiveSnapshotService.Seal(changedWithoutReseal);
        Assert.NotEqual(
            sealedArchive.ClosedSnapshotSha256,
            resealedChangedArchive.ClosedSnapshotSha256);

        store.Save(new[] { resealedChangedArchive });

        var loadedChanged = Assert.Single(store.LoadOrCreate().Archives);
        Assert.Equal(resealedChangedArchive.ClosedSnapshotSha256, loadedChanged.ClosedSnapshotSha256);
        Assert.True(ClosedArchiveSnapshotService.Matches(loadedChanged));

        store.Save(Array.Empty<ArchivedInvoiceList>());
        Assert.Empty(store.LoadOrCreate().Archives);
    }

    private static ArchivedInvoiceList ClosedArchive()
    {
        var calculation = new TaxCalculationService().Calculate(1500m, 250m, "Elektrik");
        var invoice = new InvoiceRecord(
            Guid.NewGuid(),
            new DateTime(2026, 9, 19, 10, 0, 0),
            "İlkokul",
            "Mühür Test Okulu",
            "1234567890",
            "Elektrik",
            "MÜHÜR-001",
            "1111111111",
            new DateTime(2026, 9, 1),
            new DateTime(2026, 9, 15),
            1500m,
            250m,
            calculation.VatBase,
            calculation.StampDuty,
            calculation.PaymentAmount,
            TaxRuleVersion: calculation.RuleVersion,
            TaxRuleEffectiveFrom: calculation.RuleEffectiveFrom,
            AppliedStampDutyRate: calculation.AppliedStampDutyRate);

        return new ArchivedInvoiceList(
            Guid.NewGuid(),
            "DF-MUHUR-001",
            new DateTime(2026, 9, 19, 11, 0, 0),
            "Elektrik",
            new List<InvoiceRecord> { invoice },
            AuthorityProfileDefaults.Create(),
            "HYS-2026123456",
            true,
            new DateTime(2026, 9, 19, 11, 5, 0));
    }

    private sealed class TempFolder : IDisposable
    {
        public TempFolder()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "DogruFatura_ClosedArchiveImmutabilityTests_" + Guid.NewGuid().ToString("N"));
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
