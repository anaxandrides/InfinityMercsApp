namespace InfinityMercsApp.Domain.Models.Army.Traits;

/// <summary>Order types as the API writes them on an option's orders.</summary>
public static class OrderTypes
{
    public const string Regular = "REGULAR";
    public const string Irregular = "IRREGULAR";
    public const string Impetuous = "IMPETUOUS";

    /// <summary>A tactical order: the trooper has Tactical Awareness.</summary>
    public const string Tactical = "TACTICAL";

    public const string Lieutenant = "LIEUTENANT";
}
