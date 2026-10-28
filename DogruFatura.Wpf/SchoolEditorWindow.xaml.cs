using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Windows;
using System.Windows.Input;

namespace DogruFatura.Wpf;

public partial class SchoolEditorWindow : Window
{
    private readonly SchoolRecord? _original;
    private readonly Func<SchoolRecord, string?> _save;
    public bool Saved { get; private set; }

    internal SchoolEditorWindow(SchoolRecord? original, IEnumerable<string> types, Func<SchoolRecord, string?> save)
    {
        InitializeComponent();
        _original = original; _save = save;
        Width = Math.Min(Width, SystemParameters.WorkArea.Width);
        Height = Math.Min(Height, SystemParameters.WorkArea.Height);
        MinWidth = Math.Min(MinWidth, Width); MinHeight = Math.Min(MinHeight, Height);
        TypeBox.ItemsSource = types.ToArray();
        TypeBox.SelectedItem = original?.InstitutionType ?? types.First();
        EditorHeading.Text = original is null ? "Yeni Kurum Kaydı" : "Kurum Kaydını Düzenle";
        NameBox.Text = original?.Name ?? ""; VknBox.Text = original?.Vkn ?? "";
        ElectricityBox.Text = original?.ElectricityServiceNumber ?? "";
        TelephoneBox.Text = original?.TelephoneServiceNumber ?? "";
        InternetBox.Text = original?.InternetServiceNumber ?? "";
        WaterBox.Text = original?.WaterServiceNumber ?? "";
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { Close(); e.Handled = true; } };
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (Saved) return;
        var candidate = SchoolValidationService.Normalize(new SchoolRecord(TypeBox.SelectedItem as string ?? "", NameBox.Text, VknBox.Text,
            ElectricityBox.Text, TelephoneBox.Text, InternetBox.Text, WaterBox.Text, _original?.SchoolId ?? Guid.NewGuid()));
        var error = _save(candidate);
        if (error is not null) { ErrorText.Text = error; ErrorText.Visibility = Visibility.Visible; return; }
        Saved = true; Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
