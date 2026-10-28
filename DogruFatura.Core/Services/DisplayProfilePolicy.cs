namespace DogruFatura.Core.Services;

public sealed record DisplayProfile(string Key, string Label, double Width, double Height);
public sealed record DisplaySize(double Width, double Height);

public static class DisplayProfilePolicy
{
    public static IReadOnlyList<DisplayProfile> Profiles { get; } = Array.AsReadOnly(new[]
    {
        new DisplayProfile("1024x768", "1024 × 768 · Klasik (4:3)", 1024, 768),
        new DisplayProfile("1366x768", "1366 × 768 · Standart", 1366, 768),
        new DisplayProfile("1920x1080", "1920 × 1080 · Geniş", 1920, 1080)
    });

    public static DisplayProfile Resolve(string? key) =>
        Profiles.FirstOrDefault(p => p.Key == key) ?? Profiles[1];

    // Work-area dimensions are WPF DIPs; profile dimensions describe physical pixels.
    public static DisplaySize Fit(string? key, double dpiScale, double workWidth, double workHeight)
    {
        if (!double.IsFinite(dpiScale) || dpiScale <= 0) throw new ArgumentOutOfRangeException(nameof(dpiScale));
        if (!double.IsFinite(workWidth) || workWidth <= 0) throw new ArgumentOutOfRangeException(nameof(workWidth));
        if (!double.IsFinite(workHeight) || workHeight <= 0) throw new ArgumentOutOfRangeException(nameof(workHeight));
        var profile = Resolve(key);
        return new(Math.Min(profile.Width / dpiScale, workWidth), Math.Min(profile.Height / dpiScale, workHeight));
    }
}
