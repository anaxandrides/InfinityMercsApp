using System.ComponentModel;
using InfinityMercsApp.Presentation.Themes;
using InfinityMercsApp.Presentation.Units;
using InfinityMercsApp.Presentation.Text;

namespace InfinityMercsApp.ViewModels.Components;

/// <summary>What the unit display's header shows: the unit's name, logo, icons, statline and summaries.</summary>
public interface IUnitDisplay : INotifyPropertyChanged
{
    string UnitNameHeading { get; }

    /// <summary>The logo file, or null for none.</summary>
    string? LogoSource { get; }

    UnitHeaderIcons Icons { get; }

    UnitStatLine Stats { get; }

    CompanyHeaderColors HeaderColors { get; }

    /// <summary>The peripheral's statline, or null when there is none to show.</summary>
    PeripheralStatBlock? Peripheral { get; }

    /// <summary>Whether there is a peripheral statline to show.</summary>
    bool HasPeripheral { get; }

    RichText EquipmentSummaryFormatted { get; }

    RichText SpecialSkillsSummaryFormatted { get; }
}
