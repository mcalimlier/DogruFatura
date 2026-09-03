using DogruFatura.Core.Models;

namespace DogruFatura.Core.Services;

public static class EngineSelfTest
{
    public static (bool Success, string Message) Run()
    {
        try
        {
            var service = new TaxCalculationService();
            var first = service.Calculate(4965.10m, 827.53m);
            if (!first.IsValid || first.VatBase != 4137.57m || first.StampDuty != 39.2241636m || first.PaymentAmount != 4925.8758364m)
                return (false, "4.965,10 TL örneği hesap motoru öz testini geçemedi.");
            if (NumberParser.Money(first.StampDuty) != "39,22 TL" || NumberParser.Money(first.PaymentAmount) != "4.925,88 TL")
                return (false, "Nihai iki basamaklı TL gösterimi öz testini geçemedi.");

            var second = service.Calculate(6566.20m, 1094.35m);
            if (!second.IsValid || second.VatBase != 5471.85m || second.StampDuty != 51.873138m || second.PaymentAmount != 6514.326862m)
                return (false, "6.566,20 TL örneği hesap motoru öz testini geçemedi.");

            if (service.Calculate(100m, 101m).IsValid)
                return (false, "KDV/tutar sınır kontrolü öz testini geçemedi.");

            var water = service.Calculate(4965.10m, 827.53m, "Su");
            if (!water.IsValid || water.VatBase != 4137.57m || water.StampDuty != 0m || water.PaymentAmount != 4965.10m)
                return (false, "Su faturası damga vergisi istisnası öz testini geçemedi.");
            if (NumberParser.Money(water.StampDuty) != "0,00 TL" || NumberParser.Money(water.PaymentAmount) != "4.965,10 TL")
                return (false, "Su faturası iki basamaklı TL gösterimi öz testini geçemedi.");

            var compensatedElectricity = service.Calculate(4965.10m, 827.53m, "Elektrik", 100m);
            if (!compensatedElectricity.IsValid
                || compensatedElectricity.VatBase != 4237.57m
                || compensatedElectricity.StampDuty != 40.1721636m
                || compensatedElectricity.PaymentAmount != 4924.9278364m)
                return (false, "Elektrik kesinti tazminatı hesap kuralı öz testini geçemedi.");
            if (NumberParser.Money(compensatedElectricity.StampDuty) != "40,17 TL"
                || NumberParser.Money(compensatedElectricity.PaymentAmount) != "4.924,93 TL")
                return (false, "Kesinti tazminatlı elektrik faturası iki basamaklı TL gösterimi öz testini geçemedi.");
            if (service.Calculate(100m, 10m, "Telefon", 5m).IsValid)
                return (false, "Kesinti tazminatı elektrik dışındaki fatura türünde yanlışlıkla kabul edildi.");

            var savedInvoice = new InvoiceRecord(
                Guid.NewGuid(), DateTime.Now, "İlkokul", "Örnek İlkokulu", "1234567890",
                "Elektrik", "ABC-123", "00001028900", DateTime.Today, DateTime.Today.AddDays(7),
                4965.10m, 827.53m, first.VatBase, first.StampDuty, first.PaymentAmount);
            var validWaterInvoice = savedInvoice with
            {
                Id = Guid.NewGuid(),
                InvoiceType = "Su",
                InvoiceNumber = "SU-123",
                StampDuty = water.StampDuty,
                PaymentAmount = water.PaymentAmount
            };
            if (!InvoiceValidationService.Validate(validWaterInvoice).IsValid)
                return (false, "Su faturası kayıt doğrulaması öz testini geçemedi.");
            var oldRuleWaterInvoice = validWaterInvoice with
            {
                StampDuty = first.StampDuty,
                PaymentAmount = first.PaymentAmount
            };
            if (InvoiceValidationService.Validate(oldRuleWaterInvoice).IsValid)
                return (false, "Eski su faturası hesap kuralı yanlışlıkla geçerli kabul edildi.");

            if (!InvoiceListTypePolicy.Check(new[] { savedInvoice }, "Elektrik").IsAllowed)
                return (false, "Aynı tür fatura listesi kontrolü öz testini geçemedi.");
            if (InvoiceListTypePolicy.Check(new[] { savedInvoice }, "Telefon").IsAllowed)
                return (false, "Farklı tür faturaların aynı listeye girmesi yanlışlıkla kabul edildi.");
            if (!InvoiceListTypePolicy.Check(new[] { savedInvoice }, "Su", savedInvoice.Id).IsAllowed)
                return (false, "Tek faturanın türünü değiştirme kontrolü öz testini geçemedi.");
            var duplicateCandidate = savedInvoice with
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = " abc-123 "
            };
            if (!InvoiceDuplicateService.Check(new[] { savedInvoice }, duplicateCandidate).IsDuplicate)
                return (false, "Mükerrer fatura kontrolü öz testini geçemedi.");
            if (InvoiceDuplicateService.Check(new[] { savedInvoice }, savedInvoice, savedInvoice.Id).IsDuplicate)
                return (false, "Fatura düzenleme hariç tutma kontrolü öz testini geçemedi.");
            var invoiceWithoutOptionalIdentifiers = savedInvoice with
            {
                Id = Guid.NewGuid(),
                Vkn = string.Empty,
                InvoiceNumber = string.Empty
            };
            if (!InvoiceValidationService.Validate(invoiceWithoutOptionalIdentifiers).IsValid)
                return (false, "Boş bırakılabilen VKN/fatura no kayıt doğrulaması öz testini geçemedi.");
            if (InvoiceDuplicateService.Check(new[] { savedInvoice }, invoiceWithoutOptionalIdentifiers).IsDuplicate)
                return (false, "Boş fatura numarasına yanlışlıkla mükerrer kontrolü uygulandı.");

            var compensatedInvoice = savedInvoice with
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = "ELK-TAZ-001",
                VatBase = compensatedElectricity.VatBase,
                StampDuty = compensatedElectricity.StampDuty,
                PaymentAmount = compensatedElectricity.PaymentAmount,
                InterruptionCompensationAmount = 100m
            };
            if (!InvoiceValidationService.Validate(compensatedInvoice).IsValid)
                return (false, "Kesinti tazminatlı elektrik faturası kayıt doğrulaması öz testini geçemedi.");

            var secondInvoice = savedInvoice with
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = "ABC-124"
            };
            var reportValidation = FinalReportValidationService.Validate(new[] { savedInvoice, secondInvoice });
            if (!reportValidation.IsValid || reportValidation.InvoiceType != "Elektrik")
                return (false, "Nihai çıktı tek tür doğrulaması öz testini geçemedi.");
            if (FinalReportValidationService.Validate(new[] { savedInvoice, validWaterInvoice }).IsValid)
                return (false, "Karma liste yanlışlıkla nihai çıktıya uygun kabul edildi.");

            var onePage = FinalReportPagePlanner.Plan(12);
            var twoPages = FinalReportPagePlanner.Plan(31);
            if (onePage.Count != 1 || onePage[0].Count != 12 || !onePage[0].IsFinalPage
                || twoPages.Count != 2 || twoPages.Sum(page => page.Count) != 31
                || twoPages[^1].Count > FinalReportPagePlanner.FinalPageCapacity)
                return (false, "Nihai çıktı çok sayfalı planlama öz testini geçemedi.");

            var archive = new ArchivedInvoiceList(
                Guid.NewGuid(),
                "DF-TEST-001",
                DateTime.Now,
                "Elektrik",
                new List<InvoiceRecord> { savedInvoice, secondInvoice },
                AuthorityProfileDefaults.Create());
            if (!ArchivedInvoiceListValidationService.Validate(archive).IsValid)
                return (false, "İşlem arşivi kayıt doğrulaması öz testini geçemedi.");
            if (ArchivedInvoiceListValidationService.Validate(archive with { InvoiceType = "Telefon" }).IsValid)
                return (false, "İşlem arşivinde uyuşmayan liste türü yanlışlıkla kabul edildi.");

            var controlSchool = new SchoolRecord(
                "İlkokul",
                savedInvoice.InstitutionName,
                savedInvoice.Vkn,
                savedInvoice.ServiceNumber,
                "00000000002",
                "00000000003",
                "00000000004");
            var cleanScan = ControlCenterService.Scan(
                new[] { controlSchool },
                new[] { savedInvoice },
                DateTime.Today);
            if (!cleanScan.IsClean)
                return (false, "Kontrol Merkezi geçerli kayıtları yanlışlıkla sorunlu kabul etti.");
            var optionalIdentifierSchool = controlSchool with { Vkn = string.Empty };
            var optionalIdentifierScan = ControlCenterService.Scan(
                new[] { optionalIdentifierSchool },
                new[] { invoiceWithoutOptionalIdentifiers },
                DateTime.Today);
            if (!optionalIdentifierScan.IsClean)
                return (false, "Kontrol Merkezi boş bırakılabilen VKN/fatura no alanlarını yanlışlıkla sorun kabul etti.");

            var missingServiceSchool = controlSchool with { TelephoneServiceNumber = string.Empty };
            var faultyInvoice = savedInvoice with
            {
                Id = Guid.NewGuid(),
                DueDate = savedInvoice.InvoiceDate.AddDays(-1),
                PaymentAmount = savedInvoice.PaymentAmount + 1m
            };
            var faultyScan = ControlCenterService.Scan(
                new[] { missingServiceSchool },
                new[] { savedInvoice, faultyInvoice },
                DateTime.Today);
            var expectedIssueCodes = new[]
            {
                "SCHOOL_SERVICE_MISSING_TELEFON",
                "INVOICE_DUPLICATE",
                "INVOICE_DATE_ORDER",
                "INVOICE_TAX_MISMATCH"
            };
            if (expectedIssueCodes.Any(code => faultyScan.Issues.All(issue => issue.Code != code)))
                return (false, "Kontrol Merkezi beklenen hata türlerini bulamadı.");

            var dashboardDate = new DateTime(2026, 9, 15);
            var dashboardCurrentInvoice = savedInvoice with
            {
                Id = Guid.NewGuid(),
                InstitutionName = "Birinci İlkokulu",
                InvoiceNumber = "PANEL-001",
                InvoiceDate = new DateTime(2026, 9, 2),
                DueDate = dashboardDate
            };
            var dashboardSecondInvoice = dashboardCurrentInvoice with
            {
                Id = Guid.NewGuid(),
                InstitutionName = "İkinci İlkokulu",
                InvoiceNumber = "PANEL-002",
                InvoiceDate = new DateTime(2026, 9, 3),
                DueDate = dashboardDate.AddDays(5)
            };
            var dashboardThirdInvoice = dashboardCurrentInvoice with
            {
                Id = Guid.NewGuid(),
                InstitutionName = "Üçüncü İlkokulu",
                InvoiceNumber = "PANEL-003",
                InvoiceDate = new DateTime(2026, 9, 4),
                DueDate = dashboardDate.AddDays(-1)
            };
            var dashboardArchivedInvoice = dashboardCurrentInvoice with
            {
                Id = Guid.NewGuid(),
                InstitutionName = "Dördüncü İlkokulu",
                InvoiceNumber = "PANEL-004",
                InvoiceDate = new DateTime(2026, 8, 20),
                DueDate = new DateTime(2026, 8, 27)
            };
            var dashboardOpenInvoice = dashboardCurrentInvoice with
            {
                Id = Guid.NewGuid(),
                InstitutionName = "Beşinci İlkokulu",
                InvoiceNumber = "PANEL-005",
                InvoiceDate = new DateTime(2026, 9, 5),
                DueDate = new DateTime(2026, 9, 12)
            };
            var dashboardArchive = new ArchivedInvoiceList(
                Guid.NewGuid(),
                "DF-PANEL-TEST",
                dashboardDate,
                "Elektrik",
                new List<InvoiceRecord>
                {
                    dashboardCurrentInvoice,
                    dashboardSecondInvoice,
                    dashboardThirdInvoice,
                    dashboardArchivedInvoice
                },
                AuthorityProfileDefaults.Create(),
                "HYS-202614586",
                true,
                dashboardDate);
            var dashboardOpenArchive = new ArchivedInvoiceList(
                Guid.NewGuid(),
                "DF-PANEL-ACIK",
                dashboardDate.AddDays(-10),
                "Elektrik",
                new List<InvoiceRecord> { dashboardOpenInvoice },
                AuthorityProfileDefaults.Create());
            var dashboard = DashboardStatisticsService.Build(
                new[] { dashboardOpenArchive, dashboardArchive },
                dashboardDate);
            if (dashboard.MonthlyInvoiceCount != 3
                || dashboard.MonthlyPayableTotal != 14895.30m
                || dashboard.MonthlyStampDutyTotal != 117.6724908m
                || dashboard.MonthlyPaymentTotal != 14777.6275092m)
                return (false, "Ana Ekran aylık mali özet öz testini geçemedi.");
            if (dashboard.TotalInvoiceCount != 4
                || dashboard.TotalInstitutionCount != 4
                || dashboard.InvoiceTypeDistribution.Count != 1
                || dashboard.InvoiceTypeDistribution[0].InvoiceCount != 4)
                return (false, "Ana Ekran fatura türü ve kurum özeti testini geçemedi.");
            if (dashboard.MonthlyTrend.Count != 6
                || dashboard.MonthlyTrend[^1].InvoiceCount != 3
                || dashboard.MonthlyTrend[^2].InvoiceCount != 1)
                return (false, "Ana Ekran altı aylık eğilim öz testini geçemedi.");
            if (!HysPaymentOrderService.TryNormalize("202614586", out var normalizedHys, out _)
                || normalizedHys != "HYS-202614586"
                || HysPaymentOrderService.TryNormalize("HYS-0123", out _, out _))
                return (false, "HYS Ödeme Emri Belge No öz testini geçemedi.");

            var emptyDashboard = DashboardStatisticsService.Build(
                Array.Empty<ArchivedInvoiceList>(),
                dashboardDate);
            if (emptyDashboard.TotalInvoiceCount != 0
                || emptyDashboard.MonthlyTrend.Count != 6)
                return (false, "Ana Ekran boş veri öz testini geçemedi.");

            return (true, "Vergi motoru, su istisnası, elektrik kesinti tazminatı, isteğe bağlı VKN/fatura no, tek tür liste, mükerrer kontrol, nihai çıktı, HYS arşiv kapanışı, Kontrol Merkezi ve Ana Ekran istatistik öz testi başarılı.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
