using DogruFatura.Core.Models;
using DogruFatura.Core.Services;

namespace DogruFatura.Tests;

public sealed class EmptyAuthorityProfileTests
{
    [Fact]
    public void PristineEmptyProfileSurvivesReloadAndIntegrityValidation()
    {
        var folder = Path.Combine(Path.GetTempPath(), "DogruFatura-empty-profile", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(folder, "YetkiliProfili.json");
        try
        {
            var store = new AuthorityProfileDataStore(path);
            store.Save(new AuthorityProfile("", "", "", ""));
            var read = store.LoadOrCreate();
            Assert.Null(read.WarningMessage);
            Assert.Equal("", read.Profile.RealizationOfficerName);
            Assert.False(read.Profile.IsComplete);
            DataDirectoryValidationService.ValidateFile(path);
            Assert.DoesNotContain("IsEmpty", File.ReadAllText(path), StringComparison.Ordinal);
            Assert.Empty(Directory.GetFiles(folder, "*Bozuk*"));
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    [Fact]
    public void PartiallyFilledProfileStillCannotBeSaved()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "YetkiliProfili.json");
        Assert.Throws<InvalidOperationException>(() => new AuthorityProfileDataStore(path).Save(new AuthorityProfile("Ad", "", "", "")));
        Assert.False(File.Exists(path));
    }
}
