using DogruFatura.Core.Models;
using System.Globalization;

namespace DogruFatura.Core.Services;

public sealed record InvoicePasteRow(int Line, string Institution, string Number, string Message, InvoiceRecord? Invoice)
{
    public decimal Payable => Invoice?.PayableAmount ?? 0;
    public decimal Vat => Invoice?.VatAmount ?? 0;
    public decimal Stamp => Invoice?.StampDuty ?? 0;
    public decimal Payment => Invoice?.PaymentAmount ?? 0;
}
public sealed record InvoicePastePreview(IReadOnlyList<InvoicePasteRow> Rows)
{
    public bool CanImport => Rows.Count > 0 && Rows.All(r => r.Invoice is not null);
}

/// <summary>Excel tab-separated clipboard rows. Calculated cells never enter the input contract.</summary>
public static class InvoicePasteService
{
    private static readonly string[] DateFormats = { "d.M.yyyy", "dd.MM.yyyy", "yyyy-MM-dd" };
    public const string Header = "Kurum\tTür\tFatura No\tFatura Tarihi\tSon Ödeme\tTutar\tKDV\tKesinti Tazminatı";

    public static InvoicePastePreview Preview(string text, IEnumerable<SchoolRecord> schools, IEnumerable<InvoiceRecord> existing)
    {
        var rows = new List<InvoicePasteRow>();
        var batch = existing.ToList();
        var institutions = schools.ToArray();
        var tax = new TaxCalculationService();
        var lines = text.Replace("\r", "", StringComparison.Ordinal).Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(lines[index])) continue;
            var cells = lines[index].Split('\t').Select(c => c.Trim()).ToArray();
            if (index == 0 && cells.Length >= 2 && cells[0] == "Kurum" && cells[1] == "Tür") continue;
            string Error(string message) { rows.Add(new(index + 1, cells.ElementAtOrDefault(0) ?? "", cells.ElementAtOrDefault(2) ?? "", message, null)); return message; }
            if (cells.Length is < 7 or > 8) { Error("7 sütun ve isteğe bağlı kesinti tazminatı bekleniyor."); continue; }
            var matches = institutions.Where(s => TurkishTextComparison.EqualsIgnoreCase(s.Name, cells[0])).ToArray();
            if (matches.Length != 1) { Error(matches.Length == 0 ? "Kurum bulunamadı; önce kurum kaydını ekleyin." : "Kurum adı birden fazla kayda ait; adı ayırt edin."); continue; }
            var school = matches[0];
            var kind = DomainValueText.ParseInvoiceKind(cells[1]);
            if (kind == InvoiceKind.Unknown) { Error("Fatura türü tanınmadı."); continue; }
            var type = DomainValueText.ToStorageValue(kind);
            if (!DateTime.TryParseExact(cells[3], DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                || !DateTime.TryParseExact(cells[4], DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var due))
            { Error("Tarih biçimi gün.ay.yıl olmalıdır."); continue; }
            var compensation = 0m;
            if (!NumberParser.TryNonNegativeDecimal(cells[5], out var payable) || !NumberParser.TryNonNegativeDecimal(cells[6], out var vat)
                || (cells.Length == 8 && cells[7].Length > 0 && !NumberParser.TryNonNegativeDecimal(cells[7], out compensation)))
            { Error("Tutar, KDV veya kesinti tazminatı geçersiz."); continue; }
            var calc = tax.Calculate(payable, vat, type, compensation);
            if (!calc.IsValid) { Error(calc.Message); continue; }
            var invoice = new InvoiceRecord(Guid.NewGuid(), DateTime.Now, school.InstitutionType, school.Name, school.Vkn, type,
                cells[2], school.GetServiceNumber(type), date, due, payable, vat, calc.VatBase, calc.StampDuty, calc.PaymentAmount,
                compensation, school.SchoolId, calc.RuleVersion, calc.RuleEffectiveFrom, calc.AppliedStampDutyRate);
            var validation = InvoiceValidationService.Validate(invoice);
            if (!validation.IsValid) { Error(validation.Message); continue; }
            var policy = InvoiceListTypePolicy.Check(batch, type);
            if (!policy.IsAllowed) { Error(policy.Message); continue; }
            var duplicate = InvoiceDuplicateService.Check(batch, invoice);
            if (duplicate.IsDuplicate) { Error("Mükerrer kayıt: " + duplicate.Message); continue; }
            if (InvoiceDuplicateService.CheckPossibleWhenNumberMissing(batch, invoice).IsPossibleDuplicate)
            { Error("Olası mükerrer: fatura numarasını ekleyin veya tekli girişte karşılaştırın."); continue; }
            batch.Add(invoice);
            rows.Add(new(index + 1, school.Name, cells[2], "Doğrulandı", invoice));
        }
        return new(rows);
    }
}
