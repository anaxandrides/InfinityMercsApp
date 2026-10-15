using InfinityMercsApp.Domain.Season.Rolls;

namespace InfinityMercsApp.Domain.Season.Marketplace;

/// <summary>
/// The search for a round's temporary store: a d20 on the temporary store table, where 19-20 lets the company
/// choose any store, and a store it already has means rolling again.
/// </summary>
public sealed record TemporaryStoreRoll(int Roll, TemporaryStoreRollKind Kind, string? Store)
{
    /// <summary>The highest roll that names a store; anything above lets the company choose.</summary>
    public const int HighestStoreRoll = 18;

    /// <summary>Rolls tried before giving up on finding a store the company does not have.</summary>
    public const int MaxAttempts = 50;

    /// <summary>What <paramref name="roll"/> gives a company that already has <paramref name="ownedStores"/>.</summary>
    public static TemporaryStoreRoll For(int roll, IReadOnlySet<string> ownedStores)
    {
        if (roll > HighestStoreRoll)
        {
            return new(roll, TemporaryStoreRollKind.ChooseAny, null);
        }

        var store = SeasonRollTables.TemporaryStores.Lookup(roll);
        if (string.IsNullOrWhiteSpace(store))
        {
            return new(roll, TemporaryStoreRollKind.Invalid, null);
        }

        return ownedStores.Contains(store)
            ? new(roll, TemporaryStoreRollKind.AlreadyOwned, store)
            : new(roll, TemporaryStoreRollKind.Found, store);
    }

    /// <summary>Rolls until a new store or a free choice comes up, giving up after <see cref="MaxAttempts"/> rolls.</summary>
    public static TemporaryStoreRoll Search(Func<int> rollD20, IReadOnlySet<string> ownedStores)
    {
        var last = 0;
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var result = For(rollD20(), ownedStores);
            if (result.Kind is TemporaryStoreRollKind.Found or TemporaryStoreRollKind.ChooseAny)
            {
                return result;
            }

            last = result.Roll;
        }

        return new(last, TemporaryStoreRollKind.Exhausted, null);
    }
}
