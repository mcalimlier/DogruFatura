using DogruFatura.Core.Models;

namespace DogruFatura.Core.Services;

public static class InvoiceConsistencyService
{
    public static InvoiceConsistencyResult Scan(
        IEnumerable<SchoolRecord> schools,
        IEnumerable<InvoiceRecord> activeInvoices,
        DateTime today)
    {
        ArgumentNullException.ThrowIfNull(schools);
        ArgumentNullException.ThrowIfNull(activeInvoices);

        var schoolList = schools.ToArray();
        var invoiceList = activeInvoices.ToArray();
        var issues = new List<InvoiceDataIssue>();

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
        return new InvoiceConsistencyResult(ordered);
    }

    private static void ScanSchools(IReadOnlyList<SchoolRecord> schools, List<InvoiceDataIssue> issues)
    {
        foreach (var school in schools)
        {
            if (!string.IsNullOrWhiteSpace(school.Vkn)
                && (school.Vkn.Length != 10 || !school.Vkn.All(char.IsDigit)))
            {
                issues.Add(SchoolIssue(
                    "SCHOOL_VKN_INVALID",
                    InvoiceIssueSeverity.Error,
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
                    InvoiceIssueSeverity.Warning,
                    "Hizmet Numarası",
                    $"{invoiceType} hizmet numarası eksik",
                    $"{school.Name} için {invoiceType} hizmet/tüketici numarası tanımlanmamış.",
                    school,
                    invoiceType));
            }
        }
    }

    private static void ScanActiveList(IReadOnlyList<InvoiceRecord> invoices, List<InvoiceDataIssue> issues)
    {
        var types = InvoiceListTypePolicy.GetDistinctTypes(invoices);
        if (types.Count <= 1) return;

        issues.Add(new InvoiceDataIssue(
            "ACTIVE_LIST_MIXED_TYPES",
            InvoiceIssueSeverity.Error,
            "Liste Türü",
            "Aktif listede farklı fatura türleri var",
            $"Liste yalnız tek türden oluşmalıdır. Bulunan türler: {string.Join(", ", types)}.",
            InvoiceIssueTarget.ActiveList,
            string.Empty,
            string.Join(", ", types),
            string.Empty));
    }

    private static void ScanInvoices(
        IReadOnlyList<SchoolRecord> schools,
        IReadOnlyList<InvoiceRecord> invoices,
        DateTime today,
        List<InvoiceDataIssue> issues)
    {
        foreach (var invoice in invoices)
        {
            var rules = InvoiceRulesEngine.Evaluate(invoice);

            if (rules.Has(InvoiceRuleCode.MissingInstitutionName))
            {
                issues.Add(InvoiceIssue(
                    "INVOICE_INSTITUTION_INVALID",
                    InvoiceIssueSeverity.Error,
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
                    InvoiceIssueSeverity.Error,
                    "Kurum Bilgisi",
                    "Faturanın VKN bilgisi geçersiz",
                    "VKN girildiyse 10 haneli ve yalnızca rakamlardan oluşmalıdır.",
                    invoice));
            }

            if (rules.Has(InvoiceRuleCode.InvalidInvoiceType))
            {
                issues.Add(InvoiceIssue(
                    "INVOICE_TYPE_INVALID",
                    InvoiceIssueSeverity.Error,
                    "Liste Türü",
                    "Fatura türü geçersiz",
                    "Fatura türü Elektrik, Telefon, İnternet veya Su olmalıdır.",
                    invoice));
            }

            if (rules.Has(InvoiceRuleCode.MissingServiceNumber))
            {
                issues.Add(InvoiceIssue(
                    "INVOICE_SERVICE_MISSING",
                    InvoiceIssueSeverity.Error,
                    "Hizmet Numarası",
                    "Faturanın hizmet numarası eksik",
                    "Fatura ödeme listesine geçmeden önce hizmet/tüketici numarası tamamlanmalıdır.",
                    invoice));
            }
            else if (rules.Has(InvoiceRuleCode.InvalidServiceNumberCharacters))
            {
                issues.Add(InvoiceIssue(
                    "INVOICE_SERVICE_INVALID",
                    InvoiceIssueSeverity.Error,
                    "Hizmet Numarası",
                    "Faturanın hizmet numarası geçersiz",
                    "Hizmet/tüketici numarası yalnızca rakamlardan oluşmalıdır.",
                    invoice));
            }

            if (rules.Has(InvoiceRuleCode.MissingInvoiceDates))
            {
                issues.Add(InvoiceIssue(
                    "INVOICE_DATE_MISSING",
                    InvoiceIssueSeverity.Error,
                    "Tarih",
                    "Fatura tarihleri eksik",
                    "Fatura tarihi ve son ödeme tarihi birlikte girilmelidir.",
                    invoice));
            }
            else if (rules.Has(InvoiceRuleCode.DueDateBeforeInvoiceDate))
            {
                issues.Add(InvoiceIssue(
                    "INVOICE_DATE_ORDER",
                    InvoiceIssueSeverity.Error,
                    "Tarih",
                    "Son ödeme tarihi fatura tarihinden önce",
                    $"Fatura tarihi {invoice.InvoiceDate:dd.MM.yyyy}, son ödeme tarihi {invoice.DueDate:dd.MM.yyyy}.",
                    invoice));
            }
            else if (invoice.InvoiceDate.Date > today)
            {
                issues.Add(InvoiceIssue(
                    "INVOICE_DATE_FUTURE",
                    InvoiceIssueSeverity.Warning,
                    "Tarih",
                    "Fatura tarihi gelecekte",
                    $"Fatura tarihi {invoice.InvoiceDate:dd.MM.yyyy}; kayıt tarihi ödeme öncesinde kontrol edilmelidir.",
                    invoice));
            }
            else if (invoice.DueDate.Date < today)
            {
                issues.Add(InvoiceIssue(
                    "INVOICE_DUE_DATE_PASSED",
                    InvoiceIssueSeverity.Warning,
                    "Tarih",
                    "Son ödeme tarihi geçmiş",
                    $"Son ödeme tarihi {invoice.DueDate:dd.MM.yyyy}; kayıt ödeme öncesinde kontrol edilmelidir.",
                    invoice));
            }

            if (rules.Has(InvoiceRuleCode.TaxCalculationInvalid))
            {
                issues.Add(InvoiceIssue(
                    "INVOICE_AMOUNT_INVALID",
                    InvoiceIssueSeverity.Error,
                    "Tutar / KDV",
                    "Fatura tutarı veya KDV geçersiz",
                    rules.Calculation.Message,
                    invoice));
            }
            else if (rules.Has(InvoiceRuleCode.TaxCalculationMismatch))
            {
                issues.Add(InvoiceIssue(
                    "INVOICE_TAX_MISMATCH",
                    InvoiceIssueSeverity.Error,
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
        List<InvoiceDataIssue> issues)
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
                InvoiceIssueSeverity.Error,
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
                InvoiceIssueSeverity.Warning,
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
                InvoiceIssueSeverity.Warning,
                "Hizmet Numarası",
                "Faturadaki hizmet numarası kurum kaydıyla farklı",
                $"Kurum kaydındaki numara: {currentServiceNumber}; faturadaki numara: {DisplayValue(invoice.ServiceNumber)}.",
                invoice));
        }
    }

    private static void ScanDuplicates(IReadOnlyList<InvoiceRecord> invoices, List<InvoiceDataIssue> issues)
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
                InvoiceIssueSeverity.Error,
                "Mükerrer Fatura",
                "Mükerrer fatura bulundu",
                $"{first.InvoiceNumber} numaralı fatura aynı tür ve hizmet numarasıyla {group.Count()} kez kayıtlı.",
                first));
        }
    }

    private static InvoiceDataIssue SchoolIssue(
        string code,
        InvoiceIssueSeverity severity,
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
            InvoiceIssueTarget.School,
            school.Name,
            invoiceType,
            string.Empty,
            SchoolInstitutionType: school.InstitutionType,
            SchoolName: school.Name);

    private static InvoiceDataIssue InvoiceIssue(
        string code,
        InvoiceIssueSeverity severity,
        string category,
        string title,
        string detail,
        InvoiceRecord invoice) => new(
            code,
            severity,
            category,
            title,
            detail,
            InvoiceIssueTarget.Invoice,
            invoice.InstitutionName,
            invoice.InvoiceType,
            invoice.InvoiceNumber,
            InvoiceId: invoice.Id);

    private static string DisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
}
