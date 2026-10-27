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
    private static readonly string[] Views = { "Home", "Invoice", "List", "Archive", "Control", "Schools", "Officials", "Reports", "Statistics", "About" };
    [Fact]
    public void WorkbenchOpensEveryPageWithoutScalingAndPersistsDraftsInIsolatedDataFolder()
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
                Assert.IsType<Grid>(window.FindName("AppScaleViewbox"));
                Assert.NotNull(typeof(MainWindow).GetField("_workbench", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window));
                var school = window.AllSchools.First(s => !string.IsNullOrEmpty(s.ElectricityServiceNumber));
                ((ComboBox)window.FindName("InstitutionTypeBox")).SelectedItem = school.InstitutionType;
                var institution = (ComboBox)window.FindName("InstitutionBox");
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
                Stage("draft roundtrip");
                Assert.True((bool)Invoke(window, "SaveCurrentDraft")!);
                var drafts = new InvoiceDraftStore(Path.Combine(safety.DataFolder, "FaturaTaslaklari.json")).Load();
                Assert.Single(drafts);
                Invoke(window, "SwitchDraft", new object?[] { null });
                Assert.Empty(window.ActiveInvoices);
                Invoke(window, "SwitchDraft", drafts[0]);
                Assert.Single(window.ActiveInvoices);
                var search = (TextBox)typeof(MainWindow).GetField("_listSearch", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
                search.Text = "eşleşmeyen kurum";
                Assert.Empty(((DataGrid)window.FindName("InvoicesGrid")).Items.Cast<object>());
                search.Clear();
                Assert.Single(((DataGrid)window.FindName("InvoicesGrid")).Items.Cast<object>());
                Stage("preferences roundtrip");
                Invoke(window, "SaveWorkbenchPreferences");
                var preferencesType = typeof(MainWindow).GetNestedType("WorkbenchPreferences", BindingFlags.NonPublic)!;
                Assert.NotNull(System.Text.Json.JsonSerializer.Deserialize(File.ReadAllText(Path.Combine(safety.DataFolder, "GorunumTercihleri.json")), preferencesType));
                var output = Environment.GetEnvironmentVariable("DOGRUFATURA_SCREENSHOTS");
                foreach (var view in Views)
                {
                    Stage(view);
                    typeof(MainWindow).GetMethod("ShowView", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, new object[] { view });
                    if (view == "Reports") Invoke(window, "NavReports_Click", window, new RoutedEventArgs());
                    Pump(window);
                    if (!string.IsNullOrEmpty(output)) SaveScreenshot(window, output, view);
                }
                window.Width = 900; window.Height = 620;
                Stage("narrow");
                typeof(MainWindow).GetMethod("ShowView", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, new object[] { "Invoice" });
                Pump(window);
                Assert.Equal(64, ((Grid)((Border)((Grid)window.FindName("AppScaleViewbox")).Children[0]).Child).ColumnDefinitions[0].Width.Value);
                if (!string.IsNullOrEmpty(output)) SaveScreenshot(window, output, "Invoice-narrow");
                window.Width = 1366; Pump(window);
                Assert.Equal(3, ((StackPanel)((ScrollViewer)window.FindName("InvoiceView")).Content).Children.OfType<Grid>().Single().ColumnDefinitions.Count);
                Stage("close");
                window.Close(); app.Shutdown();
                Stage("complete");
            }
            catch (Exception ex) { failure = ex; }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        });
        thread.IsBackground = true;
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        Assert.True(thread.Join(TimeSpan.FromMinutes(2)), "WPF smoke test timed out.");
        Assert.Null(failure);
    }

    private static object? Invoke(MainWindow window, string name, params object?[] arguments) =>
        typeof(MainWindow).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, arguments);

    private static void Pump(Window window)
    {
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
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
