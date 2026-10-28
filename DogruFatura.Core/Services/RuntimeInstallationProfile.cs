namespace DogruFatura.Core.Services;

public static class RuntimeInstallationProfile
{
    public static bool IsDistribution => File.Exists(Path.Combine(AppContext.BaseDirectory, "distribution.profile"));
    public static string RootDirectory => GetRoot(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), IsDistribution);
    public static string GetRoot(string desktop, bool distribution) => Path.Combine(desktop, distribution ? "DoğruFatura - Dağıtım" : "DoğruFatura");
}

public static class InitialSetupNotice
{
    public static string? GetMessage(int schoolCount, bool signatoriesComplete)
    {
        if (schoolCount > 0 && signatoriesComplete) return null;
        var missing = new List<string>();
        if (schoolCount == 0) missing.Add("Okul / Kurum bölümünden kurumlarınızı ekleyin.");
        if (!signatoriesComplete) missing.Add("Yetkili / İmza bölümünden imzacıların ad ve unvanlarını girin.");
        return "DoğruFatura'ya hoş geldiniz.\n\n" + string.Join("\n", missing) + "\n\nProgramı kullanmaya devam edebilirsiniz. Ödeme listesi hazırlamadan önce bu tanımları tamamlayın.";
    }
}
