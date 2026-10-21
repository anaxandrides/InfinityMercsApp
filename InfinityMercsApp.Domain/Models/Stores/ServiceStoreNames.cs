namespace InfinityMercsApp.Domain.Models.Stores;

/// <summary>The stores that are services rather than markets; they are always offered, after the markets.</summary>
public static class ServiceStoreNames
{
    public const string MedicalServices = "Medical Services";
    public const string AdditionalRecruitment = "Additional Recruitment";

    public static bool Contains(string? storeName) =>
        string.Equals(storeName, MedicalServices, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(storeName, AdditionalRecruitment, StringComparison.OrdinalIgnoreCase);
}
