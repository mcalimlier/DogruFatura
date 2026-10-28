using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Windows;
using System.Windows.Controls;

namespace DogruFatura.Wpf;

public partial class MainWindow
{
    private void SchoolsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateSchoolActions();

    private void UpdateSchoolActions()
    {
        if (EditSchoolButton is null) return;
        EditSchoolButton.IsEnabled = SchoolsGrid.SelectedItem is SchoolRecord;
        DeleteSchoolButton.IsEnabled = EditSchoolButton.IsEnabled;
    }

    private SchoolEditorWindow CreateSchoolEditorWindow(SchoolRecord? original) => new(original, InstitutionTypes,
        candidate => TryCommitSchool(candidate, original, out var error) ? null : error) { Owner = this };

    private void ShowSchoolEditor(SchoolRecord? original)
    {
        var editor = CreateSchoolEditorWindow(original);
        editor.ShowDialog();
        if (editor.Saved) MessageBox.Show(this, "Kurum kaydı başarıyla kaydedildi.", "Kayıt Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void NewSchool_Click(object sender, RoutedEventArgs e) => ShowSchoolEditor(null);
    private void EditSchool_Click(object sender, RoutedEventArgs e)
    {
        if (SchoolsGrid.SelectedItem is SchoolRecord selected) ShowSchoolEditor(selected);
    }

    private bool TryCommitSchool(SchoolRecord candidate, SchoolRecord? original, out string error)
    {
        candidate = SchoolValidationService.Normalize(candidate);
        var validation = SchoolValidationService.Validate(candidate, AllSchools, original);
        if (!validation.IsValid) { error = validation.Message; return false; }
        var invoiceSelection = InstitutionBox.SelectedItem as SchoolRecord;
        var index = original is null ? -1 : AllSchools.IndexOf(original);
        if (original is not null && index < 0) { error = "Düzenlenen kurum artık listede bulunmuyor."; return false; }
        if (original is null) AllSchools.Add(candidate); else AllSchools[index] = candidate;
        if (!TrySaveSchools(out error))
        {
            if (original is null) AllSchools.Remove(candidate); else AllSchools[index] = original;
            RefreshSchoolView(); UpdateSchoolActions(); return false;
        }
        AppendAudit(original is null ? AuditTrailEventType.SchoolCreated : AuditTrailEventType.SchoolUpdated,
            AuditTrailEntityType.School, candidate.SchoolId.ToString("D"), $"Kind={candidate.Kind}");
        RefreshSchoolView();
        SchoolsGrid.SelectedItem = _schoolView.Contains(candidate) ? candidate : null;
        if (SchoolsGrid.SelectedItem is not null) SchoolsGrid.ScrollIntoView(candidate);
        UpdateSchoolActions();
        SetSchoolFormStatus("Kurum kaydı başarıyla kaydedildi.", false, true);
        if (original is not null && invoiceSelection?.SchoolId == original.SchoolId)
        {
            var previous = _suppressFormEvents; _suppressFormEvents = true;
            InstitutionTypeBox.SelectedItem = candidate.InstitutionType; _suppressFormEvents = previous;
            RefreshInvoiceInstitutionChoices(candidate);
        }
        else RefreshInvoiceInstitutionChoices(invoiceSelection);
        RefreshDashboard(); error = ""; return true;
    }
}
