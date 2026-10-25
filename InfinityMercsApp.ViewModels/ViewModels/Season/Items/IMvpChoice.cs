namespace InfinityMercsApp.ViewModels.Season.Items;

/// <summary>What an XP card tells when it is made or unmade the MVP, so only one card is.</summary>
public interface IMvpChoice
{
    void MvpChanged(ExperienceUnitItem unit);
}
