namespace InfinityMercsApp.Services.Rolls;

/// <summary>Dice rolls, injectable so tests can fix the result.</summary>
public interface IDice
{
    /// <summary>A roll from 1 to 20.</summary>
    int RollD20();

    /// <summary>A roll from 1 to 6.</summary>
    int RollD6();
}
