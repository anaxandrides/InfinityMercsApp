using InfinityMercsApp.Infrastructure.Models.App;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Repositories.InMemory;

namespace InfinityMercsApp.UnitTests.Infrastructure.Providers;

public sealed class AppSettingsProviderTests
{
    private readonly InMemoryRepository _repository = new();
    private readonly AppSettingsProvider _settings;

    public AppSettingsProviderTests() => _settings = new AppSettingsProvider(_repository);

    [Fact]
    public void GetShowUnitsInInches_NothingSaved_IsInches() => Assert.True(_settings.GetShowUnitsInInches());

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetShowUnitsInInches_Saved_ReadsBack(bool inches)
    {
        _settings.SetShowUnitsInInches(!inches);
        _settings.SetShowUnitsInInches(inches);

        Assert.Equal(inches, _settings.GetShowUnitsInInches());
        Assert.Equal(1, _repository.Count<AppSetting>(_ => true));
    }

    [Fact]
    public void GetShowUnitsInInches_CentimetersInAnyCase_IsCentimeters()
    {
        _repository.Insert([new AppSetting { Key = "display_units", Value = "CENTIMETERS" }]);

        Assert.False(_settings.GetShowUnitsInInches());
    }

    [Fact]
    public void GetFeedbackApiEndpoint_NothingSaved_IsEmpty() => Assert.Equal(string.Empty, _settings.GetFeedbackApiEndpoint());

    [Fact]
    public void SetFeedbackApiEndpoint_SavedTwice_KeepsTheLatestTrimmed()
    {
        _settings.SetFeedbackApiEndpoint("https://one");
        _settings.SetFeedbackApiEndpoint("  https://two  ");

        Assert.Equal("https://two", _settings.GetFeedbackApiEndpoint());
        Assert.Equal(1, _repository.Count<AppSetting>(_ => true));
    }

    [Fact]
    public void GetMetadataFingerprint_NothingSaved_IsNull() => Assert.Null(_settings.GetMetadataFingerprint());

    [Fact]
    public void SetMetadataFingerprint_SavedTwice_KeepsTheLatest()
    {
        _settings.SetMetadataFingerprint("a");
        _settings.SetMetadataFingerprint("b");

        Assert.Equal("b", _settings.GetMetadataFingerprint());
    }

    [Fact]
    public void GetMetadataFingerprint_Blank_IsNull()
    {
        _settings.SetMetadataFingerprint(" ");

        Assert.Null(_settings.GetMetadataFingerprint());
    }

    [Fact]
    public void GetGeneratedCompaniesVersion_NothingSaved_IsNull() => Assert.Null(_settings.GetGeneratedCompaniesVersion());

    [Fact]
    public void SetGeneratedCompaniesVersion_SavedTwice_KeepsTheLatest()
    {
        _settings.SetGeneratedCompaniesVersion("1");
        _settings.SetGeneratedCompaniesVersion("2");

        Assert.Equal("2", _settings.GetGeneratedCompaniesVersion());
        Assert.Equal(1, _repository.Count<AppSetting>(_ => true));
    }
}
