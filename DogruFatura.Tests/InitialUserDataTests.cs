using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Reflection;
using System.Text.Json;

namespace DogruFatura.Tests;

public sealed class InitialUserDataTests
{
    [Fact]
    public void NoBundleDoesNotCreateUserData()
    {
        using var temp = new TestFolder();
        var target = Path.Combine(temp.Path, "User");
        Assert.False(Initialize(Path.Combine(temp.Path, "Missing"), target));
        Assert.False(Directory.Exists(target));
    }

    [Fact]
    public void PersonalBundleCopiesAllStoresAndDocumentsOnlyOnce()
    {
        using var temp = new TestFolder();
        var seed = CreateBundle(temp.Path);
        var target = Path.Combine(temp.Path, "User");
        Assert.True(Initialize(seed, target));
        foreach (var source in Directory.GetFiles(seed, "*", SearchOption.AllDirectories).Where(p => Path.GetFileName(p) != "Manifest.json"))
            Assert.Equal(File.ReadAllBytes(source), File.ReadAllBytes(Path.Combine(target, Path.GetRelativePath(seed, source))));
        var schoolPath = Path.Combine(target, "Veri", "Okullar.json");
        File.WriteAllText(schoolPath, "User's later changes");
        Assert.False(Initialize(seed, target));
        Assert.Equal("User's later changes", File.ReadAllText(schoolPath));
    }

    [Fact]
    public void ExistingPartialUserDataIsNeverMixedWithSeed()
    {
        using var temp = new TestFolder();
        var seed = CreateBundle(temp.Path);
        var target = Path.Combine(temp.Path, "User");
        Directory.CreateDirectory(Path.Combine(target, "Veri"));
        var path = Path.Combine(target, "Veri", "AktifFaturalar.json");
        File.WriteAllText(path, "existing");
        Assert.False(Initialize(seed, target));
        Assert.Equal("existing", File.ReadAllText(path));
        Assert.False(File.Exists(Path.Combine(target, "Veri", "Okullar.json")));
    }

    [Fact]
    public void CorruptBundleCannotPartiallyInitializeUserFolder()
    {
        using var temp = new TestFolder();
        var seed = CreateBundle(temp.Path);
        File.AppendAllText(Path.Combine(seed, "Arşivlenenler", "test.pdf"), "changed");
        var target = Path.Combine(temp.Path, "User");
        var error = Assert.Throws<TargetInvocationException>(() => Initialize(seed, target));
        Assert.IsType<InvalidDataException>(error.InnerException);
        Assert.False(Directory.Exists(target));
    }

    private static bool Initialize(string seed, string target)
    {
        var type = typeof(SchoolDataStore).Assembly.GetType("DogruFatura.Core.Services.InitialUserDataService");
        Assert.NotNull(type);
        return (bool)type.GetMethod("Initialize")!.Invoke(null, new object[] { seed, target })!;
    }

    private static string CreateBundle(string root)
    {
        var seed = Path.Combine(root, "Seed");
        var data = Path.Combine(seed, "Veri");
        new SchoolDataStore(Path.Combine(data, "Okullar.json")).Save(SchoolSeedData.Create());
        new InvoiceDataStore(Path.Combine(data, "AktifFaturalar.json")).Save(Array.Empty<InvoiceRecord>());
        new AuthorityProfileDataStore(Path.Combine(data, "YetkiliProfili.json")).Save(AuthorityProfileDefaults.Create());
        new ArchivedInvoiceListDataStore(Path.Combine(data, "IslemArsivi.json")).Save(Array.Empty<ArchivedInvoiceList>());
        Directory.CreateDirectory(Path.Combine(seed, "Arşivlenenler"));
        File.WriteAllText(Path.Combine(seed, "Arşivlenenler", "test.pdf"), "%PDF-test");
        var manifest = Directory.GetFiles(seed, "*", SearchOption.AllDirectories)
            .ToDictionary(p => Path.GetRelativePath(seed, p).Replace('\\', '/'), FileIntegrityService.ComputeSha256);
        File.WriteAllText(Path.Combine(seed, "Manifest.json"), JsonSerializer.Serialize(manifest));
        return seed;
    }

    private sealed class TestFolder : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DogruFatura-initial-data", Guid.NewGuid().ToString("N"));
        public void Dispose() { if (Directory.Exists(Path)) Directory.Delete(Path, true); }
    }
}
