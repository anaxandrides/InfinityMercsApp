namespace InfinityMercsApp.Services;

/// <summary>Dice rolls, injectable so tests can fix the result.</summary>
public interface IDice
{
    /// <summary>A roll from 1 to 20.</summary>
    int RollD20();
}
