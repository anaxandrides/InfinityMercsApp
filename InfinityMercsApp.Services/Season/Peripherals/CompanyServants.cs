namespace InfinityMercsApp.Services.Season.Peripherals;

/// <summary>A saved company's Servant peripherals and the Doctors and Engineers who can own one, by entry index.</summary>
public sealed record CompanyServants(IReadOnlySet<int> Servants, IReadOnlySet<int> Owners)
{
    public static CompanyServants None { get; } = new(new HashSet<int>(), new HashSet<int>());
}
