using DogruFatura.Core.Models;

namespace DogruFatura.Core.Services;

public static class ControlCenterService
{
    public static ControlScanResult Scan(
        IEnumerable<SchoolRecord> schools,
        IEnumerable<InvoiceRecord> activeInvoices,
        DateTime today)
    {
        ArgumentNullException.ThrowIfNull(schools);
        ArgumentNullException.ThrowIfNull(activeInvoices);

        var schoolList = schools.ToArray();
        var invoiceList = activeInvoices.ToArray();
        var issues = new List<ControlIssue>();

        ScanSchools(schoolList, issues);
        ScanActiveList(invoiceList, issues);
        ScanInvoices(schoolList, invoiceList, today.Date, issues);
        ScanDuplicates(invoiceList, issues);

        var ordered = issues
            .OrderBy(issue => issue.SeverityRank)
            .ThenBy(issue => issue.Category, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(issue => issue.InstitutionName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(issue => issue.InvoiceNumber, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        return new ControlScanResult(ordered);
    }

    private static void ScanSchools(IReadOnlyList<SchoolRecord> schools, List<ControlIssue> issues)
    {
        foreach (var school in schools)
        {
            if (!string.IsNullOrWhiteSpace(school.Vkn)
                && (school.Vkn.Length != 10 || !school.Vkn.All(char.IsDigit)))
            {
                issues.Add(SchoolIssue(
                    "SCHOOL_VKN_INVALID",
                    ControlIssueSeverity.Error,
                    "Kurum Bilgisi",
                    "VKN bilgisi geçersiz",
                    $"{school.Name} kaydındaki VKN girildiyse 10 haneli ve yalnızca rakamlardan oluşmalıdır.",
                    school));
            }

            foreach (var invoiceType in InvoiceRulesEngine.SupportedInvoiceTypes)
            {
                if (!string.IsNullOrWhiteSpace(school.GetServiceNumber(invoiceType))) continue;

                issues.Add(SchoolIssue(
                    $"SCHOOL_SERVICE_MISSING_{invoiceType.ToUpperInvariant()}",
                    ControlIssueSeverity.Warning,
                    "Hizmet Numarası",
                    $"{invoiceType} hizmet numarası eksik",
                    $"{school.Name} için {invoiceType} hizmet/tüketici numarası tanımlanmamış.",
                    school,
                    invoiceType));
            }
        }
    }

    private static void ScanActiveList(IReadOnlyList<InvoiceRecord> invoices, List<ControlIssue> issues)
    {
        var types = InvoiceListTypePolicy.GetDistinctTypes(invoices);
        if (types.Count <= 1) return;

        issues.Add(new ControlIssue(
            "ACTIVE_LIST_MIXED_TYPES",
            ControlIssueSeverity.Error,
            "Liste Türü",
            "Aktif listede farklı fatura türleri var",
            $"Liste yalnız tek türden oluşmalıdır. Bulunan türler: {string.Join(", ", types)}.",
            ControlIssueTarget.ActiveList,
            string.Empty,
            string.Join(", ", types),
            string.Empty));
    }

    private static void ScanInvoices(
        IReadOnlyList<SchoolRecord> schools,
        IReadOnlyList<InvoiceRecord> invoices,
        DateTime today,
        List<ControlIssue> issues)
    {
        foreach (var invoice in invoices)
        {
            var rules = InvoiceRulesEngine.Evaluate(invoice);

            if (rules.Has(InvoiceRuleCode.MissingInstitutionName))
            {
                issues.Add(InvoiceIssue(
                    "INVOICE_INSTITUTION_INVALID",
                    ControlIssueSeverity.Error,
                    "Kurum Bilgisi",
                    "Faturanın kurum bilgisi eksik",
                    "Okul/kurum adı kontrol edilmelidir.",
                    invoice));
            }
            else if (rules.Has(InvoiceRuleCode.InvalidVknCharacters)
                     || rules.Has(InvoiceRuleCode.InvalidVknLength))
            {
                issues.Add(InvoiceIssue(
                    "INVOICE_VKN_INVALID",
                    ControlIssueSeverity.Error,
                    "Kurum Bilgisi",
                    "Faturanın VKN bilgisi geçersiz",
                    "VKN girildiyse 10 haneli ve yalnızca rakamlardan oluşmalıdır.",
                    invoice));
            }

            if (rules.Has(InvoiceRuleCode.InvalidInvoiceType))
            {
                issues.Add(InvoiceIssue(
                    "INVOICE_TYPE_INVALID",
                    ControlIssueSeverity.Error,
                    "Liste Türü",
                    "Fatura türü geçersiz",
                    "Fatura türü Elektrik, Telefon, İnternet veya Su olmalıdır.",
                    invoice));
            }

            if (rules.Has(InvoiceRuleCode.MissingServiceNumber))
            {
                issues.Add(InvoiceIssue(
                    "INVOICE_SERVICE_MISSING",
                    ControlIssueSeverity.Error,
                    "Hizmet Numarası",
                    "Faturanın hizmet numarası eksik",
                    "Fatura ödeme listesine geçmeden önce hizmet/tüketici numarası tamamlanmalıdır.",
                    invoice));
            }
            else if (rules.Has(InvoiceRuleCode.InvalidServiceNumberCharacters))
            {
                issues.Add(InvoiceIssue(
                    "INVOICE_SERVICE_INVALID",
                    ControlIssueSeverity.Error,
                    "Hizmet Numarası",
                    "Faturanın hizmet numarası geçersiz",
                    "Hizmet/tüketici numarası yalnızca rakamlardan oluşmalıdır.",
                    invoice));
            }

            if (rules.Has(InvoiceRuleCode.MissingInvoiceDates))
            {
                issues.Add(InvoiceIssue(
                    "INVOICE_DATE_MISSING",
                    ControlIssueSeverity.Error,
                    "Tarih",
                    "Fatura tarihleri eksik",
                    "Fatura tarihi ve son ödeme tarihi birlikte girilmelidir.",
                    invoice));
            }
            else if (rules.Has(InvoiceRuleCode.DueDateBeforeInvoiceDate))
            {
                issues.Add(InvoiceIssue(
                    "INVOICE_DATE_ORDER",
                    ControlIssueSeverity.Error,
                    "Tarih",
                    "Son ödeme tarihi fatura tarihinden önce",
                    $"Fatura tarihi {invoice.InvoiceDate:dd.MM.yyyy}, son ödeme tarihi {invoice.DueDate:dd.MM.yyyy}.",
                    invoice));
            }
            else if (invoice.InvoiceDate.Date > today)
            {
                issues.Add(InvoiceIssue(
                    "INVOICE_DATE_FUTURE",
                    ControlIssueSeverity.Warning,
                    "Tarih",
                    "Fatura tarihi gelecekte",
                    $"Fatura tarihi {invoice.InvoiceDate:dd.MM.yyyy}; kayıt tarihi ödeme öncesinde kontrol edilmelidir.",
                    invoice));
            }
            else if (invoice.DueDate.Date < today)
            {
                issues.Add(InvoiceIssue(
                    "INVOICE_DUE_DATE_PASSED",
                    ControlIssueSeverity.Warning,
                    "Tarih",
                    "Son ödeme tarihi geçmiş",
                    $"Son ödeme tarihi {invoice.DueDate:dd.MM.yyyy}; kayıt ödeme öncesinde kontrol edilmelidir.",
                    invoice));
            }

            if (rules.Has(InvoiceRuleCode.TaxCalculationInvalid))
            {
                issues.Add(InvoiceIssue(
                    "INVOICE_AMOUNT_INVALID",
                    ControlIssueSeverity.Error,
                    "Tutar / KDV",
                    "Fatura tutarı veya KDV geçersiz",
                    rules.Calculation.Message,
                    invoice));
            }
            else if (rules.Has(InvoiceRuleCode.TaxCalculationMismatch))
            {
                issues.Add(InvoiceIssue(
                    "INVOICE_TAX_MISMATCH",
                    ControlIssueSeverity.Error,
                    "Tutar / KDV",
                    "Vergi hesabı güncel kuralla uyuşmuyor",
                    $"Beklenen ödeme {NumberParser.Money(rules.Calculation.PaymentAmount)}, kayıtlı ödeme {NumberParser.Money(invoice.PaymentAmount)}.",
                    invoice));
            }

            ScanInvoiceSchoolLink(schools, invoice, issues);
        }
    }

    private static void ScanInvoiceSchoolLink(
        IReadOnlyList<SchoolRecord> schools,
        InvoiceRecord invoice,
        List<ControlIssue> issues)
    {
        var school = invoice.SchoolId != Guid.Empty
            ? schools.FirstOrDefault(candidate => candidate.SchoolId == invoice.SchoolId)
            : schools.FirstOrDefault(candidate =>
                string.Equals(candidate.InstitutionType, invoice.InstitutionType, StringComparison.Ordinal)
                && TurkishTextComparison.EqualsIgnoreCase(candidate.Name, invoice.InstitutionName));
        if (school is null)
        {
            issues.Add(InvoiceIssue(
                "INVOICE_SCHOOL_MISSING",
                ControlIssueSeverity.Error,
                "Kurum Bilgisi",
                "Faturanın okul/kurum kaydı bulunamadı",
                "Faturayı düzenleyebilmek için bağlı okul/kurum kaydı yeniden oluşturulmalıdır.",
                invoice));
            return;
        }

        if (!string.IsNullOrWhiteSpace(school.Vkn)
            && !string.IsNullOrWhiteSpace(invoice.Vkn)
            && !string.Equals(school.Vkn.Trim(), invoice.Vkn.Trim(), StringComparison.Ordinal))
        {
            issues.Add(InvoiceIssue(
                "INVOICE_VKN_CHANGED",
                ControlIssueSeverity.Warning,
                "Kurum Bilgisi",
                "Faturadaki VKN kurum kaydıyla farklı",
                $"Kurum kaydındaki VKN: {DisplayValue(school.Vkn)}; faturadaki VKN: {DisplayValue(invoice.Vkn)}.",
                invoice));
        }

        var currentServiceNumber = school.GetServiceNumber(invoice.InvoiceType);
        if (!string.IsNullOrWhiteSpace(currentServiceNumber)
            && !string.Equals(currentServiceNumber.Trim(), invoice.ServiceNumber?.Trim(), StringComparison.Ordinal))
        {
            issues.Add(InvoiceIssue(
                "INVOICE_SERVICE_CHANGED",
                ControlIssueSeverity.Warning,
                "Hizmet Numarası",
                "Faturadaki hizmet numarası kurum kaydıyla farklı",
                $"Kurum kaydındaki numara: {currentServiceNumber}; faturadaki numara: {DisplayValue(invoice.ServiceNumber)}.",
                invoice));
        }
    }

    private static void ScanDuplicates(IReadOnlyList<InvoiceRecord> invoices, List<ControlIssue> issues)
    {
        var duplicateGroups = invoices
            .Where(invoice => !string.IsNullOrWhiteSpace(invoice.InvoiceType)
                              && !string.IsNullOrWhiteSpace(invoice.ServiceNumber)
                              && !string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
            .GroupBy(
                invoice => $"{invoice.InvoiceType.Trim()}\u001f{invoice.ServiceNumber.Trim()}\u001f{invoice.InvoiceNumber.Trim()}",
                StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1);

        foreach (var group in duplicateGroups)
        {
            var first = group.First();
            issues.Add(InvoiceIssue(
                "INVOICE_DUPLICATE",
                ControlIssueSeverity.Error,
                "Mükerrer Fatura",
                "Mükerrer fatura bulundu",
                $"{first.InvoiceNumber} numaralı fatura aynı tür ve hizmet numarasıyla {group.Count()} kez kayıtlı.",
                first));
        }
    }

    private static ControlIssue SchoolIssue(
        string code,
        ControlIssueSeverity severity,
        string category,
        string title,
        string detail,
        SchoolRecord school,
        string invoiceType = "") => new(
            code,
            severity,
            category,
            title,
            detail,
            ControlIssueTarget.School,
            school.Name,
            invoiceType,
            string.Empty,
            SchoolInstitutionType: school.InstitutionType,
            SchoolName: school.Name);

    private static ControlIssue InvoiceIssue(
        string code,
        ControlIssueSeverity severity,
        string category,
        string title,
        string detail,
        InvoiceRecord invoice) => new(
            code,
            severity,
            category,
            title,
            detail,
            ControlIssueTarget.Invoice,
            invoice.InstitutionName,
            invoice.InvoiceType,
            invoice.InvoiceNumber,
            InvoiceId: invoice.Id);

    private static string DisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
}
