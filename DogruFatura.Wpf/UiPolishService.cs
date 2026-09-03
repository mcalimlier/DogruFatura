namespace DogruFatura.Wpf;

/// <summary>
/// MainWindow'un iş kurallarına dokunmadan final kullanıcı deneyimi katmanını kurar.
/// Görsel cila, responsive yerleşim, erişilebilirlik ve performans ayrı servislerde tutulur.
/// </summary>
internal static class UiPolishService
{
    public static void Apply(MainWindow window)
    {
        ResponsiveLayoutService.Apply(window);
        TextSharpnessService.Apply(window);

        window.Loaded += (_, _) =>
        {
            UiVisualPolishService.Apply(window);
            UiPerformanceService.Apply(window);
            UiAccessibilityService.Apply(window);
            UnsavedChangesGuard.Attach(window);
        };
    }
}
