namespace InfinityMercsApp.Presentation.Weapons;

/// <summary>A row of a weapon card's special rules: two side by side, the last row may have one.</summary>
public sealed record WeaponCardRuleRow(LinkedText Left, LinkedText? Right)
{
    public bool HasRight => Right is not null;
}
