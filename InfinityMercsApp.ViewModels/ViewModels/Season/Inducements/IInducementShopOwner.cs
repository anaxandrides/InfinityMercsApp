namespace InfinityMercsApp.ViewModels.Season.Inducements;

/// <summary>What the inducement shop tells its page after anything is bought, hired or returned.</summary>
public interface IInducementShopOwner
{
    void ShopChanged();
}
