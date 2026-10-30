using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using DogruFatura.Wpf;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DogruFatura.Tests;

public sealed class WorkbenchWindowTests
{
    private static readonly string[] Views = { "Home", "Invoice", "List", "Archive", "Schools", "Officials", "Statistics", "Security", "About" };
    private static readonly string[] ProfileViews = { "Home", "Invoice", "List", "Archive", "Statistics" };
    private static readonly (string, decimal)[] SmallSliceFixtures = { ("Su", 12m), ("Telefon", 6m), ("İnternet", 1m) };
    private static readonly int[] ComparisonYears = { 2025, 2026 };
    [Fact]
    public void ApprovedPanelsPreservePacketsAndAdaptToAllDisplayProfiles()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            var root = Path.Combine(Path.GetTempPath(), "DogruFatura-visual", Guid.NewGuid().ToString("N"));
            var progressFolder = Environment.GetEnvironmentVariable("DOGRUFATURA_SCREENSHOTS") ?? Path.GetTempPath();
            Directory.CreateDirectory(progressFolder);
            void Stage(string stage) => File.AppendAllText(Path.Combine(progressFolder, "wpf-smoke-progress.log"), stage + Environment.NewLine);
            try
            {
                Stage("resources");
                var app = new App(); app.InitializeComponent();
                // Surface UI exceptions to xUnit instead of opening the production error dialog in unattended tests.
                app.DispatcherUnhandledException -= (DispatcherUnhandledExceptionEventHandler)Delegate.CreateDelegate(typeof(DispatcherUnhandledExceptionEventHandler), app,
                    typeof(App).GetMethod("App_DispatcherUnhandledException", BindingFlags.Instance | BindingFlags.NonPublic)!);
                var safety = new ApplicationSafetyService(Path.Combine(root, "documents"), Path.Combine(root, "desktop"));
                var schools = new SchoolDataStore(Path.Combine(safety.DataFolder, "Okullar.json"));
                var invoices = new InvoiceDataStore(Path.Combine(safety.DataFolder, "AktifFaturalar.json"));
                var authority = new AuthorityProfileDataStore(Path.Combine(safety.DataFolder, "YetkiliProfili.json"));
                var archives = new ArchivedInvoiceListDataStore(Path.Combine(safety.DataFolder, "IslemArsivi.json"));
                var storage = new DocumentArchiveStorageService(Path.Combine(root, "desktop"));
                var tax = new TaxCalculationService(); var pdf = new FinalReportPdfService();
                var state = typeof(MainWindow).Assembly.GetType("DogruFatura.Wpf.MainWindowStartupStateLoader")!.GetMethod("Load")!
                    .Invoke(null, new object[] { safety, schools, invoices, authority, archives });
                Stage("construct window");
                var window = (MainWindow)Activator.CreateInstance(typeof(MainWindow), BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new object[] { tax, schools, invoices, authority, archives, safety, new FinalReportDocumentService(), pdf, storage,
                        new HistoricalPdfImportService(tax), new ArchivePdfSynchronizationService(storage, pdf.Save),
                        new AuditTrailStore(Path.Combine(safety.DataFolder, "DenetimKaydi.jsonl")), new TransactionJournalStore(Path.Combine(safety.DataFolder, "IslemGunlugu.jsonl")), state! }, null)!;
                typeof(MainWindow).Assembly.GetType("DogruFatura.Wpf.UiPolishService")!.GetMethod("Apply")!.Invoke(null, new object[] { window });
                Stage("show window");
                window.Width = 1366; window.Height = 768; window.Show();
                Pump(window);
                Stage("fill form");
                Invoke(window, "LoadAuthorityProfileToForm", new AuthorityProfile("", "", "", ""));
                Assert.Equal("", ((TextBox)window.FindName("RealizationOfficerNameBox")).Text);
                Assert.Equal("", ((TextBox)window.FindName("SpendingAuthorityNameBox")).Text);
                Assert.DoesNotContain("ÖZENÇ", ReadText(window, "RealizationPreviewNameText"), StringComparison.Ordinal);
                Assert.False(((Button)window.FindName("SaveAuthorityProfileButton")).IsEnabled);
                Invoke(window, "LoadAuthorityProfileToForm", authority.LoadOrCreate().Profile);
                Assert.IsType<Grid>(window.FindName("AppScaleViewbox"));
                Assert.Null(window.FindName("NavReports"));
                Assert.Null(window.FindName("NavControl"));
                var home = (ScrollViewer)window.FindName("HomeView");
                Assert.Empty(Descendants(home).OfType<Button>());
                var school = window.AllSchools.First(s => !string.IsNullOrEmpty(s.ElectricityServiceNumber));
                ((ComboBox)window.FindName("InstitutionTypeBox")).SelectedItem = school.InstitutionType;
                var institution = (ComboBox)window.FindName("InstitutionBox");
                institution.ApplyTemplate();
                var schoolSearchEditor = (TextBox)institution.Template.FindName("PART_EditableTextBox", institution);
                schoolSearchEditor.Text = "31 Ağustos"; Pump(window);
                var august = Assert.Single(institution.Items.Cast<SchoolRecord>());
                Assert.Equal("31 Ağustos Ortaokulu", august.Name);
                institution.SelectedItem = august;
                Assert.Equal("Ortaokul", ((ComboBox)window.FindName("InstitutionTypeBox")).SelectedItem);
                ((ComboBox)window.FindName("InstitutionTypeBox")).SelectedItem = school.InstitutionType;
                institution.SelectedItem = school;
                ((ComboBox)window.FindName("InvoiceTypeBox")).SelectedItem = "Elektrik";
                ((TextBox)window.FindName("InvoiceNumberBox")).Text = "ORNEK-00001";
                ((DatePicker)window.FindName("InvoiceDatePicker")).SelectedDate = DateTime.Today.AddDays(-5);
                ((DatePicker)window.FindName("DueDatePicker")).SelectedDate = DateTime.Today.AddDays(3);
                ((TextBox)window.FindName("PayableAmountBox")).Text = "1.200,00";
                ((TextBox)window.FindName("VatAmountBox")).Text = "200,00";
                Assert.True(((Button)window.FindName("AddInvoiceButton")).IsEnabled);
                ((Button)window.FindName("AddInvoiceButton")).RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                Assert.Single(window.ActiveInvoices);
                Assert.Single(invoices.LoadOrCreate().Invoices);
                ((DataGrid)window.FindName("InvoicesGrid")).SelectedIndex = 0;
                Invoke(window, "EditSelectedInvoice_Click", window, new RoutedEventArgs());
                ((TextBox)window.FindName("PayableAmountBox")).Text = "1.500,00";
                Invoke(window, "CancelInvoiceEdit_Click", window, new RoutedEventArgs());
                Assert.Equal(1200m, Assert.Single(invoices.LoadOrCreate().Invoices).PayableAmount);
                Assert.Equal(Visibility.Collapsed, ((Button)window.FindName("CancelInvoiceEditButton")).Visibility);
                Assert.Equal("1", ReadText(window, "HomeInvoiceCountText"));
                Assert.Equal("1", ReadText(window, "HomePageCountText"));
                Assert.Equal("1", ReadText(window, "HomeInstitutionCountText"));
                Assert.Equal("0", ReadText(window, "HomeOverdueCountText"));
                Assert.Equal(14.5, ((ComboBox)window.FindName("InstitutionBox")).FontSize);
                Assert.Equal(36, ((ComboBox)window.FindName("InstitutionBox")).Height);
                Assert.Null(window.FindName("InvoicePreviewExpander"));
                Assert.Null(window.FindName("FinalReportViewer"));
                Assert.Equal(window.FindResource("PremiumFieldComboBox"), ((ComboBox)window.FindName("InstitutionBox")).Style);
                var date = (DatePicker)window.FindName("InvoiceDatePicker"); date.ApplyTemplate();
                var dateText = (System.Windows.Controls.Primitives.DatePickerTextBox)date.Template.FindName("PART_TextBox", date);
                Assert.Equal("#FF082744", dateText.Background.ToString(System.Globalization.CultureInfo.InvariantCulture));
                var dateButton = (Button)date.Template.FindName("PART_Button", date);
                dateButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)); Pump(window);
                var calendarPopup = (System.Windows.Controls.Primitives.Popup)date.Template.FindName("PART_Popup", date);
                Assert.True(calendarPopup.IsOpen);
                Assert.IsType<Calendar>(calendarPopup.Child);
                calendarPopup.IsOpen = false;
                Assert.DoesNotContain("Fatura tarihini seçin", ReadText(window, "InvoiceSaveStatusText"), StringComparison.Ordinal);
                Invoke(window, "SaveFinalReportPdf_Click", window, new RoutedEventArgs());
                var savedPdf = Assert.Single(Directory.GetFiles(storage.ActiveOutputDirectory, "*.pdf"));
                Assert.True(new FileInfo(savedPdf).Length > 0);
                Assert.Equal(storage.ActiveOutputDirectory, typeof(MainWindow).GetProperty("ListOutputFolder", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window));
                File.Delete(savedPdf);
                Stage("school dialog cancel/save/edit");
                var schoolCount = window.AllSchools.Count;
                var originalSchoolFile = File.ReadAllText(schools.DataFilePath);
                var schoolCancel = (Window)Invoke(window, "CreateSchoolEditorWindow", new object?[] { null })!;
                schoolCancel.Show(); Pump(schoolCancel);
                ((TextBox)schoolCancel.FindName("NameBox")).Text = "Kaydedilmeyecek Kurum";
                Click((Button)schoolCancel.FindName("CancelButton"));
                Assert.Equal(schoolCount, window.AllSchools.Count);
                Assert.Equal(originalSchoolFile, File.ReadAllText(schools.DataFilePath));
                var schoolNew = (Window)Invoke(window, "CreateSchoolEditorWindow", new object?[] { null })!;
                schoolNew.Show(); Pump(schoolNew);
                ((TextBox)schoolNew.FindName("NameBox")).Text = "Deneme Kurumu";
                ((ComboBox)schoolNew.FindName("WaterRecipientBox")).SelectedIndex = 1;
                if (Environment.GetEnvironmentVariable("DOGRUFATURA_SCREENSHOTS") is { } schoolShots) SaveScreenshot(schoolNew, schoolShots, "School-new");
                Click((Button)schoolNew.FindName("SaveButton"));
                Assert.Equal(schoolCount + 1, window.AllSchools.Count);
                var addedSchool = window.AllSchools.Single(item => item.Name == "Deneme Kurumu");
                Assert.Equal(WaterPaymentRecipient.Company, addedSchool.WaterRecipient);
                ((DataGrid)window.FindName("SchoolsGrid")).SelectedItem = addedSchool;
                Assert.True(((Button)window.FindName("EditSchoolButton")).IsEnabled);
                var schoolEdit = (Window)Invoke(window, "CreateSchoolEditorWindow", addedSchool)!;
                schoolEdit.Show(); Pump(schoolEdit);
                Assert.Equal("Deneme Kurumu", ((TextBox)schoolEdit.FindName("NameBox")).Text);
                Assert.Equal(1, ((ComboBox)schoolEdit.FindName("WaterRecipientBox")).SelectedIndex);
                ((TextBox)schoolEdit.FindName("NameBox")).Text = "Düzenlenmiş Deneme Kurumu";
                ((TextBox)schoolEdit.FindName("ElectricityBox")).Text = "00009901";
                Click((Button)schoolEdit.FindName("SaveButton"));
                Assert.Equal(addedSchool.SchoolId, window.AllSchools.Single(item => item.Name == "Düzenlenmiş Deneme Kurumu").SchoolId);
                Assert.Equal(schoolCount + 1, schools.LoadOrCreate().Schools.Count);
                File.Move(schools.DataFilePath, schools.DataFilePath + ".held");
                Directory.CreateDirectory(schools.DataFilePath);
                try
                {
                    var schoolFailure = (Window)Invoke(window, "CreateSchoolEditorWindow", new object?[] { null })!;
                    schoolFailure.Show(); Pump(schoolFailure);
                    ((TextBox)schoolFailure.FindName("NameBox")).Text = "Yazılamayacak Kurum";
                    Click((Button)schoolFailure.FindName("SaveButton"));
                    Assert.True(schoolFailure.IsVisible);
                    Assert.Equal(Visibility.Visible, ((TextBlock)schoolFailure.FindName("ErrorText")).Visibility);
                    Assert.Equal(schoolCount + 1, window.AllSchools.Count);
                    schoolFailure.Close();
                }
                finally { Directory.Delete(schools.DataFilePath); File.Move(schools.DataFilePath + ".held", schools.DataFilePath); }
                var version = (TextBox)window.FindName("HeaderVersionText");
                Assert.True(version.IsReadOnly); version.SelectAll(); Assert.True(version.SelectionLength > 0);
                Assert.DoesNotContain("preview", version.Text, StringComparison.OrdinalIgnoreCase);
                Assert.Null(window.FindName("ArchiveHysNumberBox"));
                Assert.Null(window.FindName("SchoolNameEditorBox"));
                Stage("preferences roundtrip");
                Invoke(window, "SaveWorkbenchPreferences");
                var preferencesType = typeof(MainWindow).GetNestedType("WorkbenchPreferences", BindingFlags.NonPublic)!;
                Assert.NotNull(System.Text.Json.JsonSerializer.Deserialize(File.ReadAllText(Path.Combine(safety.DataFolder, "GorunumTercihleri.json")), preferencesType));
                var output = Environment.GetEnvironmentVariable("DOGRUFATURA_SCREENSHOTS");
                foreach (var view in Views)
                {
                    Stage(view);
                    typeof(MainWindow).GetMethod("ShowView", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, new object[] { view });

                    Pump(window);
                    if (!string.IsNullOrEmpty(output)) SaveScreenshot(window, output, view);
                }
                foreach (var profile in DisplayProfilePolicy.Profiles)
                {
                    Stage("profile " + profile.Key);
                    var previousWidth = window.Width;
                    ((ComboBox)window.FindName("DisplayProfileBox")).SelectedItem = profile;
                    Assert.Equal(previousWidth, window.Width);
                    var apply = (Button)window.FindName("ApplyDisplayProfileButton");
                    if (apply.IsEnabled) Invoke(window, "ApplyDisplayProfile_Click", window, new RoutedEventArgs());
                    Pump(window);
                    Assert.False(apply.IsEnabled);
                    Assert.True(window.Width <= SystemParameters.WorkArea.Width);
                    Assert.True(window.Height <= SystemParameters.WorkArea.Height);
                    foreach (var view in ProfileViews)
                    {
                        Invoke(window, "ShowView", view); Pump(window);
                        if (window.FindName(view + "View") is Grid page && page.Parent is ScrollViewer scroll)
                            Assert.True(page.ActualWidth <= scroll.ViewportWidth + 1, $"{view} exceeds viewport at {profile.Key}");
                        if (!string.IsNullOrEmpty(output)) SaveScreenshot(window, output, view + "-" + profile.Key);
                    }
                    var expectedColumns = window.ActualWidth - (window.ActualWidth < 1200 ? 64 : 210) - 66 < 950 ? 1 : 3;
                    Assert.Equal(expectedColumns, ((StackPanel)((ScrollViewer)window.FindName("InvoiceView")).Content).Children.OfType<Grid>().Single().ColumnDefinitions.Count);
                }
                Stage("failed PDF preserves invoices");
                // A directory occupying the exact output filename deterministically prevents PDF creation.
                var blockedPath = storage.GetActiveOutputPath(window.ActiveInvoices.ToArray(), "Elektrik", DateTime.Today);
                Directory.CreateDirectory(blockedPath);
                Assert.False((bool)Invoke(window, "TryArchiveActiveList", false)!);
                Assert.Single(window.ActiveInvoices);
                Assert.Single(invoices.LoadOrCreate().Invoices);
                Assert.Empty(archives.LoadOrCreate().Archives);
                Directory.Delete(blockedPath);
                Stage("packet roundtrip");
                Assert.True((bool)Invoke(window, "TryArchiveActiveList", false)!);
                Assert.Empty(window.ActiveInvoices);
                Assert.Empty(invoices.LoadOrCreate().Invoices);
                var packet = Assert.Single(archives.LoadOrCreate().Archives);
                Assert.False(packet.IsClosed);
                Assert.Single(packet.Invoices);
                Assert.False(string.IsNullOrWhiteSpace(packet.PdfFileName));
                Assert.True(File.Exists(Path.Combine(storage.ActiveOutputDirectory, packet.PdfFileName)));
                Assert.Equal("1", ReadText(window, "HomePendingText"));
                Invoke(window, "ShowView", "Archive"); Pump(window);
                if (!string.IsNullOrEmpty(output)) SaveScreenshot(window, output, "Archive-packet");
                Stage("archive dialog cancel/close/reopen");
                var pendingBytes = File.ReadAllText(archives.DataFilePath);
                var pendingDialog = (Window)Invoke(window, "CreateArchiveActionWindow", packet)!;
                pendingDialog.Show(); Pump(pendingDialog);
                ((TextBox)pendingDialog.FindName("ArchiveHysBox")).Text = "HYS-2026123456";
                Assert.Equal(4, ((System.Windows.Controls.Primitives.UniformGrid)pendingDialog.FindName("ArchiveSummaryCards")).Children.Count);
                if (!string.IsNullOrEmpty(output)) SaveScreenshot(pendingDialog, output, "HYS-close-dialog");
                Click((Button)pendingDialog.FindName("ArchiveCancelButton"));
                Assert.Equal(pendingBytes, File.ReadAllText(archives.DataFilePath));
                pendingDialog = (Window)Invoke(window, "CreateArchiveActionWindow", packet)!;
                pendingDialog.Show(); Pump(pendingDialog);
                ((TextBox)pendingDialog.FindName("ArchiveHysBox")).Text = "hatalı";
                Click((Button)pendingDialog.FindName("ArchiveCommitButton"));
                Assert.True(pendingDialog.IsVisible);
                Assert.Equal(pendingBytes, File.ReadAllText(archives.DataFilePath));
                ((TextBox)pendingDialog.FindName("ArchiveHysBox")).Text = "HYS-2026123456";
                var activePdfBeforeClose = Path.Combine(storage.ActiveOutputDirectory, packet.PdfFileName);
                File.Move(archives.DataFilePath, archives.DataFilePath + ".held"); Directory.CreateDirectory(archives.DataFilePath);
                try
                {
                    Click((Button)pendingDialog.FindName("ArchiveCommitButton"));
                    Assert.True(pendingDialog.IsVisible);
                    Assert.False(Assert.Single(window.ArchivedLists).IsClosed);
                    Assert.True(File.Exists(activePdfBeforeClose));
                    Assert.Empty(Directory.GetFiles(storage.ArchivedOutputDirectory, "*.pdf"));
                }
                finally { Directory.Delete(archives.DataFilePath); File.Move(archives.DataFilePath + ".held", archives.DataFilePath); }
                Click((Button)pendingDialog.FindName("ArchiveCommitButton"));
                var closed = Assert.Single(archives.LoadOrCreate().Archives);
                Assert.True(closed.IsClosed); Assert.True(closed.HasClosedSnapshot);
                var closedPdf = Path.Combine(storage.ArchivedOutputDirectory, closed.PdfFileName);
                Assert.True(File.Exists(closedPdf));
                closed = window.ArchivedLists.Single();
                if (!string.IsNullOrEmpty(output)) { Pump(window); SaveScreenshot(window, output, "Archive-closed"); }
                var duplicate = packet with { Id = Guid.NewGuid(), ArchiveNumber = "DUP-CHECK" };
                window.ArchivedLists.Add(duplicate);
                Assert.False((bool)Invoke(window, "TryCloseArchive", duplicate, "HYS-2026123456", null)!);
                window.ArchivedLists.Remove(duplicate);
                var closedBytes = File.ReadAllText(archives.DataFilePath);
                window.ActiveInvoices.Add(packet.Invoices[0]); invoices.Save(window.ActiveInvoices);
                var activeBytes = File.ReadAllText(invoices.DataFilePath);
                Assert.False((bool)Invoke(window, "TryRestoreArchiveToActive", closed, null)!);
                Assert.Equal(activeBytes, File.ReadAllText(invoices.DataFilePath));
                Assert.Equal(closedBytes, File.ReadAllText(archives.DataFilePath));
                Assert.True(File.Exists(closedPdf));
                window.ActiveInvoices.Clear(); invoices.Save(window.ActiveInvoices);
                var closedDialog = (Window)Invoke(window, "CreateArchiveActionWindow", closed)!;
                closedDialog.Show(); Pump(closedDialog);
                Assert.Equal("Düzenle", ((Button)closedDialog.FindName("ArchiveCommitButton")).Content);
                if (!string.IsNullOrEmpty(output)) SaveScreenshot(closedDialog, output, "HYS-reopen-dialog");
                Click((Button)closedDialog.FindName("ArchiveCancelButton"));
                Assert.Equal(closedBytes, File.ReadAllText(archives.DataFilePath));
                File.Move(archives.DataFilePath, archives.DataFilePath + ".held"); Directory.CreateDirectory(archives.DataFilePath);
                try
                {
                    Assert.False((bool)Invoke(window, "TryRestoreArchiveToActive", closed, null)!);
                    Assert.Empty(window.ActiveInvoices);
                    Assert.Empty(invoices.LoadOrCreate().Invoices);
                    Assert.True(Assert.Single(window.ArchivedLists).IsClosed);
                    Assert.True(File.Exists(closedPdf));
                }
                finally { Directory.Delete(archives.DataFilePath); File.Move(archives.DataFilePath + ".held", archives.DataFilePath); }
                closedDialog = (Window)Invoke(window, "CreateArchiveActionWindow", closed)!;
                closedDialog.Show(); Pump(closedDialog);
                Click((Button)closedDialog.FindName("ArchiveCommitButton"));
                Assert.Single(window.ActiveInvoices);
                Assert.Single(invoices.LoadOrCreate().Invoices);
                Assert.Empty(archives.LoadOrCreate().Archives);
                Assert.False(File.Exists(closedPdf));
                Stage("statistics rows and scrolling");
                foreach (var year in ComparisonYears)
                {
                    var sourceInvoice = packet.Invoices[0];
                    var invoice = sourceInvoice with { Id = Guid.NewGuid(), InvoiceDate = new DateTime(year, 1, 15), DueDate = new DateTime(year, 1, 25) };
                    window.ArchivedLists.Add(packet with { Id = Guid.NewGuid(), ArchiveNumber = "TEST-" + year, IsClosed = true,
                        HysPaymentOrderDocumentNumber = "HYS-" + year, Invoices = new() { invoice } });
                }
                foreach (var (kind, amount) in SmallSliceFixtures)
                {
                    var calc = tax.Calculate(amount, 0m, kind);
                    var invoice = packet.Invoices[0] with { Id = Guid.NewGuid(), InvoiceType = kind, PayableAmount = amount, VatAmount = 0,
                        VatBase = calc.VatBase, StampDuty = calc.StampDuty, PaymentAmount = calc.PaymentAmount };
                    window.ArchivedLists.Add(packet with { Id = Guid.NewGuid(), ArchiveNumber = "SMALL-" + kind, InvoiceType = kind,
                        IsClosed = true, HysPaymentOrderDocumentNumber = "HYS-SMALL-" + kind, Invoices = new() { invoice } });
                }
                ((ComboBox)window.FindName("DisplayProfileBox")).SelectedItem = DisplayProfilePolicy.Profiles[1];
                Invoke(window, "ApplyDisplayProfile_Click", window, new RoutedEventArgs());
                Invoke(window, "ShowView", "Statistics"); Pump(window);
                var stats = (Grid)window.FindName("StatisticsView"); var statsScroll = (ScrollViewer)stats.Parent;
                Assert.True(statsScroll.ExtentHeight > statsScroll.ViewportHeight);
                var labels = (Canvas)window.FindName("StatisticsDonutLabelsCanvas");
                Assert.Equal(4, labels.Children.OfType<Border>().Count());
                Assert.Equal(3, labels.Children.OfType<System.Windows.Shapes.Polyline>().Count());
                Assert.Equal(3, labels.Children.OfType<System.Windows.Shapes.Ellipse>().Count());
                Assert.Equal(3, labels.Children.OfType<System.Windows.Shapes.Polyline>().Select(line => line.Stroke.ToString(System.Globalization.CultureInfo.InvariantCulture)).Distinct().Count());
                var comparison = (Window)Invoke(window, "CreateInstitutionChangesWindow")!;
                comparison.Show(); Pump(comparison);
                var sheet = Descendants(comparison).OfType<DataGrid>().Single();
                Assert.Single(sheet.Items.Cast<object>());
                Assert.Contains(sheet.Columns, c => Equals(c.Header, "Önceki Tutar"));
                if (!string.IsNullOrEmpty(output)) SaveScreenshot(comparison, output, "Institution-comparison");
                comparison.Close();
                if (!string.IsNullOrEmpty(output)) SaveScreenshot(window, output, "Statistics-data");
                statsScroll.ScrollToBottom(); Pump(window);
                if (!string.IsNullOrEmpty(output)) SaveScreenshot(window, output, "Statistics-bottom");
                Stage("water recipient entry and edit");
                window.ActiveInvoices.Clear();
                var waterSchool = window.AllSchools.First(s => !string.IsNullOrEmpty(s.WaterServiceNumber));
                ((ComboBox)window.FindName("InstitutionTypeBox")).SelectedItem = waterSchool.InstitutionType;
                institution.SelectedItem = waterSchool;
                ((ComboBox)window.FindName("InvoiceTypeBox")).SelectedItem = "Su";
                var waterRecipient = (ComboBox)window.FindName("WaterRecipientBox");
                waterRecipient.SelectedIndex = 0;
                var vatBox = (TextBox)window.FindName("VatAmountBox");
                Assert.False(vatBox.IsEnabled);
                Assert.Equal("0,00", vatBox.Text);
                Assert.Contains("Belediyeye ödeme", ReadText(window, "TaxPolicyDescription"), StringComparison.Ordinal);
                ((DatePicker)window.FindName("InvoiceDatePicker")).SelectedDate = DateTime.Today;
                ((DatePicker)window.FindName("DueDatePicker")).SelectedDate = DateTime.Today;
                ((TextBox)window.FindName("InvoiceNumberBox")).Text = "SU-BELEDIYE";
                ((TextBox)window.FindName("PayableAmountBox")).Text = "1.200,00";
                Invoke(window, "AddInvoice_Click", window, new RoutedEventArgs());
                var municipality = Assert.Single(window.ActiveInvoices);
                Assert.Equal(WaterPaymentRecipient.Municipality, municipality.WaterRecipient);
                Assert.Equal(0m, municipality.VatAmount);
                Assert.Equal(0m, municipality.StampDuty);
                Assert.Equal(1200m, municipality.PaymentAmount);
                waterRecipient.SelectedIndex = 1;
                Assert.True(vatBox.IsEnabled);
                Assert.Contains("Su şirketine ödeme", ReadText(window, "TaxPolicyDescription"), StringComparison.Ordinal);
                ((DatePicker)window.FindName("InvoiceDatePicker")).SelectedDate = DateTime.Today;
                ((DatePicker)window.FindName("DueDatePicker")).SelectedDate = DateTime.Today;
                ((TextBox)window.FindName("InvoiceNumberBox")).Text = "SU-SIRKET";
                ((TextBox)window.FindName("PayableAmountBox")).Text = "1.200,00";
                vatBox.Text = "200,00";
                foreach (var profile in DisplayProfilePolicy.Profiles)
                {
                    ((ComboBox)window.FindName("DisplayProfileBox")).SelectedItem = profile;
                    Invoke(window, "ApplyDisplayProfile_Click", window, new RoutedEventArgs());
                    Invoke(window, "ShowView", "Invoice"); Pump(window);
                    Assert.Equal(Visibility.Visible, ((StackPanel)window.FindName("WaterRecipientPanel")).Visibility);
                    if (!string.IsNullOrEmpty(output)) SaveScreenshot(window, output, "Water-" + profile.Key);
                }
                Invoke(window, "AddInvoice_Click", window, new RoutedEventArgs());
                Assert.Equal(2, window.ActiveInvoices.Count);
                var company = window.ActiveInvoices.Last();
                Assert.Equal(WaterPaymentRecipient.Company, company.WaterRecipient);
                Assert.Equal(9.48m, company.StampDuty);
                Assert.Equal(1190.52m, company.PaymentAmount);
                Assert.Equal(2, invoices.LoadOrCreate().Invoices.Count);
                ((DataGrid)window.FindName("InvoicesGrid")).SelectedIndex = 1;
                Invoke(window, "EditSelectedInvoice_Click", window, new RoutedEventArgs());
                Assert.Equal(1, waterRecipient.SelectedIndex);
                Assert.Equal("200,00", vatBox.Text);
                Click((Button)window.FindName("CancelInvoiceEditButton")); Pump(window);
                Stage("close");
                // Avoid the intentional active-list exit prompt during unattended cleanup.
                // All persistence assertions above use isolated files and are complete.
                window.ActiveInvoices.Clear();
                window.Close(); app.Shutdown();
                Stage("complete");
            }
            catch (Exception ex) { failure = ex; }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        });
        thread.IsBackground = true;
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        Assert.True(thread.Join(TimeSpan.FromMinutes(5)), "WPF smoke test timed out.");
        Assert.Null(failure);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private static string ReadText(MainWindow window, string name) => window.FindName(name) switch
    {
        TextBox text => text.Text, TextBlock text => text.Text, _ => throw new InvalidOperationException(name)
    };
    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

    private static object? Invoke(MainWindow window, string name, params object?[] arguments) =>
        typeof(MainWindow).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, arguments);

    private static void Pump(Window window)
    {
        window.UpdateLayout();
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }

    private static void SaveScreenshot(Window window, string folder, string name)
    {
        Directory.CreateDirectory(folder);
        var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(folder, name + ".png")); encoder.Save(stream);
    }
}
