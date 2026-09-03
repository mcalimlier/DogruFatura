using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Diagnostics;

namespace DogruFatura.Tests;

public sealed class PerformanceTests
{
    [Fact]
    public void ControlCenter_HandlesOneThousandInvoicesWithinReasonableTime()
    {
        var school = School();
        var invoices = Enumerable.Range(1, 1000).Select(index => Invoice(index, school.Name)).ToArray();
        var watch = Stopwatch.StartNew();

        var result = ControlCenterService.Scan(new[] { school }, invoices, new DateTime(2026, 9, 3));

        watch.Stop();
        Assert.NotNull(result);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(8), $"Kontrol Merkezi 1000 kayıtta {watch.Elapsed.TotalSeconds:0.00} sn sürdü.");
    }

    [Fact]
    public void Dashboard_HandlesOneThousandArchivedInvoicesWithinReasonableTime()
    {
        var school = School();
        var archives = Enumerable.Range(0, 200)
            .Select(archiveIndex =>
            {
                var invoices = Enumerable.Range(1, 5)
                    .Select(row => Invoice(archiveIndex * 5 + row, school.Name))
                    .ToList();
                return new ArchivedInvoiceList(
                    Guid.NewGuid(),
                    $"ARS-{archiveIndex + 1:0000}",
                    new DateTime(2026, 9, 3),
                    "Elektrik",
                    invoices,
                    AuthorityProfileDefaults.Create(),
                    $"HYS-2026{archiveIndex + 100000:000000}",
                    true,
                    new DateTime(2026, 9, 3),
                    $"HYS-2026{archiveIndex + 100000:000000}.pdf");
            })
            .ToArray();
        var watch = Stopwatch.StartNew();

        var statistics = DashboardStatisticsService.Build(archives, new DateTime(2026, 9, 3));

        watch.Stop();
        Assert.Equal(1000, statistics.TotalInvoiceCount);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5), $"Ana panel 1000 kayıtta {watch.Elapsed.TotalSeconds:0.00} sn sürdü.");
    }

    private static SchoolRecord School() => new(
        "İlkokul", "Performans Test Kurumu", "1234567890",
        "123456789012345678", "2222222222", "3333333333", "4444444444");

    private static InvoiceRecord Invoice(int index, string schoolName)
    {
        const decimal payable = 1500m;
        const decimal vat = 250m;
        var calculation = new TaxCalculationService().Calculate(payable, vat, "Elektrik");
        return new InvoiceRecord(
            Guid.Parse($"00000000-0000-0000-0001-{index:000000000000}"),
            new DateTime(2026, 9, 3), "İlkokul", schoolName, "1234567890",
            "Elektrik", $"FTR-{index:000000}", "123456789012345678",
            new DateTime(2026, 9, 1), new DateTime(2026, 9, 15),
            payable, vat, calculation.VatBase, calculation.StampDuty, calculation.PaymentAmount);
    }
}
