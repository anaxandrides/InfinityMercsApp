namespace InfinityMercsApp.Presentation.Units;

/// <summary>A peripheral's statline, shown under its controller's in the unit display.</summary>
/// <param name="Equipment">The equipment as plain text, "-" for none.</param>
/// <param name="Skills">The skills as plain text, "-" for none.</param>
public sealed record PeripheralStatBlock(
    string NameHeading,
    UnitStatLine Stats,
    string Equipment,
    string Skills,
    FormattedString EquipmentFormatted,
    FormattedString SkillsFormatted)
{
    public bool HasEquipment => !string.IsNullOrWhiteSpace(Equipment) && Equipment != "-";

    public bool HasSkills => !string.IsNullOrWhiteSpace(Skills) && Skills != "-";
}
