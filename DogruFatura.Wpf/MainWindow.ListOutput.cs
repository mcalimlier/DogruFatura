using DogruFatura.Core.Services;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DogruFatura.Wpf;

public partial class MainWindow
{
    private string? _lastListOutputPath;

    private FinalReportData? CreateActiveOutputData()
    {
        var validation = FinalReportValidationService.Validate(ActiveInvoices);
        if (!validation.IsValid || validation.InvoiceType is null)
        {
            SetInvoiceListStatus(validation.Message, true);
            return null;
        }
        return new FinalReportData(ActiveInvoices.ToArray(), validation.InvoiceType, DateTime.Today,
            new ReportSignature("Gerçekleştirme Görevlisi", _authorityProfile.RealizationOfficerName, _authorityProfile.RealizationOfficerTitle),
            new ReportSignature("Harcama Yetkilisi", _authorityProfile.SpendingAuthorityName, _authorityProfile.SpendingAuthorityTitle));
    }

    private string SaveManagedReportPdf(FinalReportData data)
    {
        var path = _documentArchiveStorageService.GetActiveOutputPath(data.Invoices, data.InvoiceType, data.ReportDate);
        _finalReportPdfService.Save(data, path);
        _lastListOutputPath = path;
        return path;
    }

    private void SaveFinalReportPdf_Click(object sender, RoutedEventArgs e)
    {
        var data = CreateActiveOutputData();
        if (data is null) return;
        try { SetInvoiceListStatus("PDF Kaydedildi: " + Path.GetFileName(SaveManagedReportPdf(data)), false, true); }
        catch (Exception ex) { SetInvoiceListStatus("PDF Kaydedilemedi: " + ex.Message, true); }
    }

    private void PrintFinalReport_Click(object sender, RoutedEventArgs e)
    {
        var data = CreateActiveOutputData();
        if (data is null) return;
        try
        {
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() != true) return;
            var path = SaveManagedReportPdf(data);
            var document = _finalReportDocumentService.Build(data);
            dialog.PrintDocument(document.DocumentPaginator, $"DoğruFatura - {data.InvoiceType} Fatura Ödeme Listesi");
            SetInvoiceListStatus("Liste Yazdırıldı; PDF Kaydı: " + path, false, true);
        }
        catch (Exception ex) { SetInvoiceListStatus("Yazdırma Tamamlanamadı: " + ex.Message, true); }
    }

    private string ListOutputFolder => _lastListOutputPath is { } path && Directory.Exists(Path.GetDirectoryName(path))
        ? Path.GetDirectoryName(path)! : _documentArchiveStorageService.ActiveOutputDirectory;

    private void OpenOutputFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(ListOutputFolder);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ListOutputFolder) { UseShellExecute = true });
        }
        catch (Exception ex) { SetInvoiceListStatus("Klasör Açılamadı: " + ex.Message, true); }
    }
}
