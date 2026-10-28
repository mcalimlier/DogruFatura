namespace DogruFatura.Wpf;

public sealed record ExitConfirmationPrompt(string Title, string Message);

public static class ExitConfirmationPolicy
{
    public static ExitConfirmationPrompt? Create(bool hasUnsavedFormChanges, int activeInvoiceCount)
    {
        if (!hasUnsavedFormChanges && activeInvoiceCount <= 0) return null;
        var messages = new List<string>();
        if (hasUnsavedFormChanges)
            messages.Add("Formlarda kaydedilmemiş değişiklikler var. Program kapatılırsa bu form değişiklikleri kaybolacak.");
        if (activeInvoiceCount > 0)
            messages.Add($"Aktif listenizde {activeInvoiceCount} fatura var. Bu faturalar diske kaydedildi; programı yeniden açtığınızda aktif listede kalacak.");
        messages.Add("Yine de programdan çıkmak istiyor musunuz?");
        return new ExitConfirmationPrompt("DoğruFatura - Çıkış Onayı", string.Join("\n\n", messages));
    }
}
