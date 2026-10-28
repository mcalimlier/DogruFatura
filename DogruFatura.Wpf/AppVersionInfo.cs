using System.Reflection;

namespace DogruFatura.Wpf;

public static class AppVersionInfo
{
    public static string InformationalVersion
    {
        get
        {
            var value = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;
            if (string.IsNullOrWhiteSpace(value)) return "1.1.0";
            var plusIndex = value.IndexOf('+');
            return plusIndex >= 0 ? value[..plusIndex] : value;
        }
    }

    public static string DisplayVersion
    {
        get
        {
            var value = InformationalVersion;
            if (value.EndsWith("-rc1", StringComparison.OrdinalIgnoreCase))
                return $"v{value[..^4]} RC1";
            return $"v{value}";
        }
    }

    public static bool IsPreRelease => InformationalVersion.Contains('-', StringComparison.Ordinal);
    public static string ShortDisplayVersion => "v" + InformationalVersion.Split('-')[0];
}
