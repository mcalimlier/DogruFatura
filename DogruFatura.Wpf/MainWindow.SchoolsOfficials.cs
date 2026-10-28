using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DogruFatura.Wpf;

public partial class MainWindow
{
    private bool FilterSchool(object item)
    {
        if (item is not SchoolRecord school) return false;

        var selectedKind = DomainValueText.ParseInstitutionKind(
            SchoolFilterTypeBox.SelectedItem as string);
        if (selectedKind != InstitutionKind.Unknown && school.Kind != selectedKind)
            return false;

        var search = SchoolSearchBox.Text.Trim();
        if (search.Length == 0) return true;

        return school.InstitutionType.Contains(search, StringComparison.CurrentCultureIgnoreCase)
               || school.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase)
               || school.Vkn.Contains(search, StringComparison.OrdinalIgnoreCase)
               || school.ElectricityServiceNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
               || school.TelephoneServiceNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
               || school.InternetServiceNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
               || school.WaterServiceNumber.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private void SchoolSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressSchoolEvents) return;
        RefreshSchoolView();
    }

    private void SchoolFilterTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSchoolEvents) return;
        RefreshSchoolView();
    }

    private void RefreshSchoolView()
    {
        _schoolView.Refresh();
        var visibleCount = _schoolView.Cast<object>().Count();
        SchoolCountText.Text = $"{visibleCount} / {AllSchools.Count} kayıt";
    }

    private void DeleteSchool_Click(object sender, RoutedEventArgs e)
    {
        if (SchoolsGrid.SelectedItem is not SchoolRecord schoolToDelete) return;
        var confirmation = MessageBox.Show(this,
            $"'{schoolToDelete.Name}' kaydı kalıcı olarak silinsin mi?",
            "DoğruFatura - Kurum Silme Onayı",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (confirmation != MessageBoxResult.Yes) return;

        var invoiceSelection = InstitutionBox.SelectedItem as SchoolRecord;
        var deletedInvoiceSelection = ReferenceEquals(invoiceSelection, schoolToDelete);
        var deletedIndex = AllSchools.IndexOf(schoolToDelete);
        AllSchools.Remove(schoolToDelete);

        if (!TrySaveSchools(out var errorMessage))
        {
            AllSchools.Insert(Math.Clamp(deletedIndex, 0, AllSchools.Count), schoolToDelete);
            SetSchoolFormStatus(errorMessage, true);
            RefreshSchoolView();
            return;
        }

        AppendAudit(
            AuditTrailEventType.SchoolDeleted,
            AuditTrailEntityType.School,
            schoolToDelete.SchoolId.ToString("D"),
            $"Kind={schoolToDelete.Kind}");

        RefreshSchoolView();
        UpdateSchoolActions();
        SetSchoolFormStatus("Kurum kaydı silindi.", false, true);
        RefreshInvoiceInstitutionChoices(deletedInvoiceSelection ? null : invoiceSelection);
    }

    private bool TrySaveSchools(out string errorMessage)
    {
        try
        {
            _schoolDataStore.Save(AllSchools);
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = "Kayıt kalıcı dosyaya yazılamadı. Değişiklik geri alındı: " + ex.Message;
            return false;
        }
    }

    private void SetSchoolFormStatus(string message, bool isError, bool isSuccess = false)
    {
        SchoolFormStatusText.Text = message;
        if (isError)
        {
            SchoolFormStatusText.Foreground = Brushes.MistyRose;
            SchoolFormStatusBorder.Background = new SolidColorBrush(Color.FromRgb(104, 38, 49));
            SchoolFormStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(218, 91, 91));
        }
        else if (isSuccess)
        {
            SchoolFormStatusText.Foreground = new SolidColorBrush(Color.FromRgb(221, 246, 231));
            SchoolFormStatusBorder.Background = new SolidColorBrush(Color.FromRgb(20, 77, 68));
            SchoolFormStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(55, 143, 103));
        }
        else
        {
            SchoolFormStatusText.Foreground = new SolidColorBrush(Color.FromRgb(216, 228, 239));
            SchoolFormStatusBorder.Background = new SolidColorBrush(Color.FromRgb(18, 59, 91));
            SchoolFormStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(54, 88, 116));
        }
    }

    private void AuthorityField_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressAuthorityEvents) return;

        UpdateAuthorityProfilePreview();
        var draft = ReadAuthorityProfileForm().Normalize();
        SaveAuthorityProfileButton.IsEnabled = draft.IsComplete;

        if (!draft.IsComplete)
        {
            SetAuthorityStatus("İki yetkilinin ad soyad ve unvan alanları zorunludur.", true);
            return;
        }

        if (draft == _authorityProfile)
        {
            SetAuthorityStatus("Yetkili bilgileri kayıtlı ve nihai çıktıya hazır.", false, true);
            return;
        }

        SetAuthorityStatus("Kaydedilmemiş değişiklikler var. Çıktılara aktarmak için KAYDET'e basın.");
    }

    private void SaveAuthorityProfile_Click(object sender, RoutedEventArgs e)
    {
        var profile = ReadAuthorityProfileForm().Normalize();
        if (!profile.IsComplete)
        {
            SetAuthorityStatus("Kayıt yapılamadı: bütün ad soyad ve unvan alanlarını doldurun.", true);
            return;
        }

        try
        {
            _authorityProfileDataStore.Save(profile);
            AppendAudit(
                AuditTrailEventType.AuthorityProfileUpdated,
                AuditTrailEntityType.AuthorityProfile,
                "authority-profile");
            _authorityProfile = profile;
            LoadAuthorityProfileToForm(profile);
            SetAuthorityStatus("Yetkili profili kaydedildi; önizleme, PDF ve yazdırma güncellendi.", false, true);
        }
        catch (Exception ex)
        {
            SetAuthorityStatus("Yetkili profili kaydedilemedi: " + ex.Message, true);
            MessageBox.Show(this, "Yetkili profili kaydedilemedi:\n\n" + ex.Message,
                "DoğruFatura - Yetkili Profili", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RestoreAuthorityDefaults_Click(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(this,
            "Yetkili bilgileri onaylı başlangıç değerlerine döndürülecek ve kaydedilecek. Devam edilsin mi?",
            "DoğruFatura - Varsayılan Yetkili Profili",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes) return;

        var defaultProfile = AuthorityProfileDefaults.Create();
        try
        {
            _authorityProfileDataStore.Save(defaultProfile);
            AppendAudit(
                AuditTrailEventType.AuthorityProfileDefaultsRestored,
                AuditTrailEntityType.AuthorityProfile,
                "authority-profile");
            _authorityProfile = defaultProfile;
            LoadAuthorityProfileToForm(defaultProfile);
            SetAuthorityStatus("Varsayılan yetkili bilgileri geri yüklendi ve kaydedildi.", false, true);
        }
        catch (Exception ex)
        {
            SetAuthorityStatus("Varsayılan bilgiler kaydedilemedi: " + ex.Message, true);
            MessageBox.Show(this, "Varsayılan yetkili profili kaydedilemedi:\n\n" + ex.Message,
                "DoğruFatura - Yetkili Profili", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private AuthorityProfile ReadAuthorityProfileForm() => new(
        RealizationOfficerNameBox.Text,
        RealizationOfficerTitleBox.Text,
        SpendingAuthorityNameBox.Text,
        SpendingAuthorityTitleBox.Text);

    private void LoadAuthorityProfileToForm(AuthorityProfile profile)
    {
        _suppressAuthorityEvents = true;
        RealizationOfficerNameBox.Text = profile.RealizationOfficerName;
        RealizationOfficerTitleBox.Text = profile.RealizationOfficerTitle;
        SpendingAuthorityNameBox.Text = profile.SpendingAuthorityName;
        SpendingAuthorityTitleBox.Text = profile.SpendingAuthorityTitle;
        _suppressAuthorityEvents = false;

        SaveAuthorityProfileButton.IsEnabled = profile.IsComplete;
        UpdateAuthorityProfilePreview();
        SetAuthorityStatus(profile.IsComplete ? "Yetkili bilgileri kayıtlı ve nihai çıktıya hazır." : "Ödeme listesi hazırlamadan önce imzacıların ad ve unvanlarını tanımlayın.", false, profile.IsComplete);
    }

    private void UpdateAuthorityProfilePreview()
    {
        RealizationPreviewNameText.Text = PreviewText(RealizationOfficerNameBox.Text, "Ad soyad");
        RealizationPreviewTitleText.Text = PreviewText(RealizationOfficerTitleBox.Text, "Unvan");
        SpendingPreviewNameText.Text = PreviewText(SpendingAuthorityNameBox.Text, "Ad soyad");
        SpendingPreviewTitleText.Text = PreviewText(SpendingAuthorityTitleBox.Text, "Unvan");
    }

    private static string PreviewText(string value, string placeholder) =>
        string.IsNullOrWhiteSpace(value) ? $"— {placeholder} —" : value.Trim();

    private void SetAuthorityStatus(string message, bool isError = false, bool isSuccess = false)
    {
        AuthorityStatusText.Text = message;
        if (isError)
        {
            AuthorityStatusText.Foreground = Brushes.MistyRose;
            AuthorityStatusBorder.Background = new SolidColorBrush(Color.FromRgb(95, 29, 37));
            AuthorityStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(169, 67, 77));
        }
        else if (isSuccess)
        {
            AuthorityStatusText.Foreground = new SolidColorBrush(Color.FromRgb(221, 246, 231));
            AuthorityStatusBorder.Background = new SolidColorBrush(Color.FromRgb(19, 81, 54));
            AuthorityStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(45, 138, 87));
        }
        else
        {
            AuthorityStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 231, 179));
            AuthorityStatusBorder.Background = new SolidColorBrush(Color.FromRgb(92, 67, 18));
            AuthorityStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(181, 128, 20));
        }
    }
}
