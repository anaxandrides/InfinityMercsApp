namespace InfinityMercsApp.Services;

/// <summary>Dice rolls, injectable so tests can fix the result.</summary>
public interface IDice
{
    /// <summary>A roll from 1 to 20.</summary>
    int RollD20();
}

internal sealed class Dice : IDice
{
    public int RollD20() => Random.Shared.Next(1, 21);
}
