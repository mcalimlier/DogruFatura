using DogruFatura.Core.Services;
using DogruFatura.Core.Models;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace DogruFatura.Wpf;

public partial class MainWindow
{    private void InvalidateFinalReportPreview()
    {
        _finalReportDocument = null;
        _finalReportData = null;
        _isWholePageReportView = false;
        UpdateWholePageReportButton();
        FinalReportViewer.Document = null;
        ReportPageCountText.Text = "0 sayfa";
        SetReportToolsEnabled(false);
        ReportEmptyState.Visibility = Visibility.Visible;
    }

    private void SetReportToolsEnabled(bool isEnabled)
    {
        SavePdfButton.IsEnabled = isEnabled;
        PrintReportButton.IsEnabled = isEnabled;
        WholePageReportButton.IsEnabled = isEnabled;
    }

    private bool RefreshFinalReportPreview()
    {
        var reportInvoices = _openedArchiveForReport?.Invoices ?? ActiveInvoices.ToList();
        var reportAuthority = _openedArchiveForReport?.AuthorityProfile ?? _authorityProfile;
        var reportDate = _openedArchiveForReport?.ArchivedAt.Date ?? DateTime.Today;
        var validation = FinalReportValidationService.Validate(reportInvoices);
        ReportInvoiceCountText.Text = $"{reportInvoices.Count} kayıt";
        ReportInvoiceTypeText.Text = InvoiceListTypePolicy.GetListTypeLabel(reportInvoices);
        ReportPaymentTotalText.Text = NumberParser.Money(reportInvoices.Sum(invoice => invoice.PaymentAmount));

        if (!validation.IsValid || validation.InvoiceType is null)
        {
            InvalidateFinalReportPreview();
            SetReportStatus(validation.Message, true);
            return false;
        }

        try
        {
            _finalReportData = new FinalReportData(
                reportInvoices.ToArray(),
                validation.InvoiceType,
                reportDate,
                new ReportSignature(
                    "Gerçekleştirme Görevlisi",
                    reportAuthority.RealizationOfficerName,
                    reportAuthority.RealizationOfficerTitle),
                new ReportSignature(
                    "Harcama Yetkilisi",
                    reportAuthority.SpendingAuthorityName,
                    reportAuthority.SpendingAuthorityTitle));
            _finalReportDocument = _finalReportDocumentService.Build(_finalReportData);
            FinalReportViewer.Document = _finalReportDocument;

            var pageCount = FinalReportPagePlanner.Plan(reportInvoices.Count).Count;
            ReportPageCountText.Text = $"{pageCount} sayfa";
            SetReportToolsEnabled(true);
            ReportEmptyState.Visibility = Visibility.Collapsed;
            SetReportStatus(
                _openedArchiveForReport is null
                    ? validation.Message
                    : $"{_openedArchiveForReport.ArchiveNumber} arşiv kaydı görüntüleniyor; aktif liste değiştirilmedi.",
                false,
                true);
            _isWholePageReportView = false;
            UpdateWholePageReportButton();

            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                try
                {
                    if (!_isWholePageReportView)
                        FinalReportViewer.FitToWidth();
                }
                catch
                {
                    // Belge görüntüleyici henüz ölçülmediyse varsayılan yakınlaştırma kullanılır.
                }
            }));
            return true;
        }
        catch (Exception ex)
        {
            InvalidateFinalReportPreview();
            SetReportStatus("Nihai çıktı önizlemesi oluşturulamadı: " + ex.Message, true);
            return false;
        }
    }

    private bool EnsureFinalReportPreview() =>
        _finalReportDocument is not null && _finalReportData is not null || RefreshFinalReportPreview();

    private void SaveFinalReportPdf_Click(object sender, RoutedEventArgs e)
    {
        if (!RefreshFinalReportPreview() || _finalReportData is null) return;

        try
        {
            var filePath = SaveManagedReportPdf(_finalReportData);
            if (_openedArchiveForReport is { } archive)
                AppendAudit(AuditTrailEventType.ReportExported, AuditTrailEntityType.Archive, archive.Id.ToString("D"), "Format=PDF");
            SetReportStatus($"PDF başarıyla kaydedildi: {filePath}", false, true);
        }
        catch (Exception ex)
        {
            SetReportStatus("PDF kaydedilemedi: " + ex.Message, true);
            MessageBox.Show(this, "PDF kaydedilemedi:\n\n" + ex.Message,
                "DoğruFatura - PDF", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PrintFinalReport_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureFinalReportPreview() || _finalReportDocument is null) return;

        try
        {
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() != true) return;

            var storedPdfPath = SaveManagedReportPdf(_finalReportData!);

            dialog.PrintDocument(
                _finalReportDocument.DocumentPaginator,
                $"DoğruFatura - {_finalReportData?.InvoiceType} Fatura Ödeme Listesi");
            SetReportStatus($"Nihai liste yazdırıldı; PDF kaydı korundu: {storedPdfPath}", false, true);
        }
        catch (Exception ex)
        {
            SetReportStatus("Yazdırma işlemi tamamlanamadı: " + ex.Message, true);
            MessageBox.Show(this, "Yazdırma işlemi tamamlanamadı:\n\n" + ex.Message,
                "DoğruFatura - Yazdır", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string SaveManagedReportPdf(FinalReportData data)
    {
        string filePath;
        if (_openedArchiveForReport is { IsClosed: true } closedArchive
            && !string.IsNullOrWhiteSpace(closedArchive.HysPaymentOrderDocumentNumber))
        {
            filePath = _documentArchiveStorageService.GetArchivedOutputPath(
                closedArchive.HysPaymentOrderDocumentNumber);
            if (File.Exists(filePath)) return filePath;
        }
        else
        {
            filePath = _documentArchiveStorageService.GetActiveOutputPath(
                data.Invoices,
                data.InvoiceType,
                data.ReportDate);
        }

        _finalReportPdfService.Save(data, filePath);
        return filePath;
    }

    private void WholePageFinalReport_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureFinalReportPreview()) return;

        try
        {
            if (_isWholePageReportView)
            {
                FinalReportViewer.FitToWidth();
                _isWholePageReportView = false;
            }
            else
            {
                FinalReportViewer.FitToHeight();
                _isWholePageReportView = true;
            }

            UpdateWholePageReportButton();
        }
        catch (Exception ex)
        {
            _isWholePageReportView = false;
            UpdateWholePageReportButton();
            SetReportStatus("Belge görünümü değiştirilemedi: " + ex.Message, true);
        }
    }

    private void UpdateWholePageReportButton()
    {
        WholePageReportButton.Content = _isWholePageReportView ? "GERİ DÖN" : "TAM SAYFA";
        WholePageReportButton.ToolTip = _isWholePageReportView
            ? "Geniş belge görünümüne dön"
            : "Sayfanın tamamını göster";
    }

    private void SetReportStatus(string message, bool isError, bool isSuccess = false)
    {
        ReportStatusText.Text = message;
        if (isError)
        {
            ReportStatusText.Foreground = Brushes.MistyRose;
            ReportStatusBorder.Background = new SolidColorBrush(Color.FromRgb(104, 38, 49));
            ReportStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(218, 91, 91));
        }
        else if (isSuccess)
        {
            ReportStatusText.Foreground = new SolidColorBrush(Color.FromRgb(221, 246, 231));
            ReportStatusBorder.Background = new SolidColorBrush(Color.FromRgb(20, 77, 68));
            ReportStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(55, 143, 103));
        }
        else
        {
            ReportStatusText.Foreground = new SolidColorBrush(Color.FromRgb(216, 228, 239));
            ReportStatusBorder.Background = new SolidColorBrush(Color.FromRgb(18, 59, 91));
            ReportStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(54, 88, 116));
        }
    }
}
