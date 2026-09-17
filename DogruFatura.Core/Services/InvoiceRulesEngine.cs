using DogruFatura.Core.Models;

namespace DogruFatura.Core.Services;

public enum InvoiceRuleCode
{
    None = 0,
    InvalidId,
    MissingInstitutionType,
    MissingInstitutionName,
    InvalidVknCharacters,
    InvalidVknLength,
    InvalidInvoiceType,
    MissingServiceNumber,
    InvalidServiceNumberCharacters,
    ServiceNumberTooLong,
    InvoiceNumberTooLong,
    MissingCreatedAt,
    MissingInvoiceDates,
    DueDateBeforeInvoiceDate,
    TaxCalculationInvalid,
    TaxCalculationMismatch
}

public sealed record InvoiceRuleViolation(InvoiceRuleCode Code, string Message);

public sealed record InvoiceRulesEvaluation(
    IReadOnlyList<InvoiceRuleViolation> Violations,
    TaxCalculationResult Calculation)
{
    public bool IsValid => Violations.Count == 0;

    public bool Has(InvoiceRuleCode code) =>
        Violations.Any(violation => violation.Code == code);

    public InvoiceRuleViolation? Find(InvoiceRuleCode code) =>
        Violations.FirstOrDefault(violation => violation.Code == code);
}

public static class InvoiceRulesEngine
{
    private static readonly IReadOnlyList<string> InvoiceTypes = Array.AsReadOnly(
        new[] { "Elektrik", "Telefon", "İnternet", "Su" });

    public static IReadOnlyList<string> SupportedInvoiceTypes => InvoiceTypes;

    public static bool IsSupportedInvoiceType(string? invoiceType) =>
        invoiceType is not null && InvoiceTypes.Contains(invoiceType, StringComparer.Ordinal);

    public static InvoiceRulesEvaluation Evaluate(InvoiceRecord invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var violations = new List<InvoiceRuleViolation>();

        if (invoice.Id == Guid.Empty)
            Add(violations, InvoiceRuleCode.InvalidId, "Fatura kayıt kimliği geçersiz.");

        if (string.IsNullOrWhiteSpace(invoice.InstitutionType))
            Add(violations, InvoiceRuleCode.MissingInstitutionType, "Kurum türü seçilmelidir.");

        if (string.IsNullOrWhiteSpace(invoice.InstitutionName))
            Add(violations, InvoiceRuleCode.MissingInstitutionName, "Okul / kurum seçilmelidir.");

        if (!string.IsNullOrWhiteSpace(invoice.Vkn))
        {
            if (!invoice.Vkn.All(char.IsDigit))
                Add(violations, InvoiceRuleCode.InvalidVknCharacters, "VKN girildiğinde yalnızca rakamlardan oluşmalıdır.");

            if (invoice.Vkn.Length != 10)
                Add(violations, InvoiceRuleCode.InvalidVknLength, "VKN girildiğinde 10 haneli olmalıdır.");
        }

        if (!IsSupportedInvoiceType(invoice.InvoiceType))
            Add(violations, InvoiceRuleCode.InvalidInvoiceType, "Geçerli bir fatura türü seçilmelidir.");

        if (string.IsNullOrWhiteSpace(invoice.ServiceNumber))
        {
            Add(violations, InvoiceRuleCode.MissingServiceNumber, "Seçilen fatura türünün hizmet numarası eksik.");
        }
        else
        {
            if (!invoice.ServiceNumber.All(char.IsDigit))
                Add(violations, InvoiceRuleCode.InvalidServiceNumberCharacters, "Hizmet numarası yalnızca rakamlardan oluşmalıdır.");

            if (invoice.ServiceNumber.Length > 50)
                Add(violations, InvoiceRuleCode.ServiceNumberTooLong, "Hizmet numarası en fazla 50 haneli olabilir.");
        }

        if (invoice.InvoiceNumber.Length > 100)
            Add(violations, InvoiceRuleCode.InvoiceNumberTooLong, "Fatura numarası en fazla 100 karakter olabilir.");

        if (invoice.CreatedAt == default)
            Add(violations, InvoiceRuleCode.MissingCreatedAt, "Fatura tarih bilgileri eksik.");

        if (invoice.InvoiceDate == default || invoice.DueDate == default)
        {
            Add(violations, InvoiceRuleCode.MissingInvoiceDates, "Fatura tarih bilgileri eksik.");
        }
        else if (invoice.DueDate.Date < invoice.InvoiceDate.Date)
        {
            Add(violations, InvoiceRuleCode.DueDateBeforeInvoiceDate, "Son ödeme tarihi fatura tarihinden önce olamaz.");
        }

        var calculation = new TaxCalculationService().Calculate(
            invoice.PayableAmount,
            invoice.VatAmount,
            invoice.InvoiceType,
            invoice.InterruptionCompensationAmount);

        if (!calculation.IsValid)
        {
            Add(violations, InvoiceRuleCode.TaxCalculationInvalid, calculation.Message);
        }
        else if (invoice.VatBase != calculation.VatBase
                 || invoice.StampDuty != calculation.StampDuty
                 || invoice.PaymentAmount != calculation.PaymentAmount)
        {
            Add(
                violations,
                InvoiceRuleCode.TaxCalculationMismatch,
                "Faturanın vergi hesap sonuçları güncel hesap motoruyla uyuşmuyor.");
        }

        return new InvoiceRulesEvaluation(violations, calculation);
    }

    private static void Add(
        ICollection<InvoiceRuleViolation> violations,
        InvoiceRuleCode code,
        string message) =>
        violations.Add(new InvoiceRuleViolation(code, message));
}
