using System.Collections.ObjectModel;
using System.Globalization;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;
using Resume = InfinityMercsApp.Domain.Models.Army.Resume;

namespace InfinityMercsApp.ViewModels;

public partial class ViewerViewModel
{
    private void PopulateUnitStatsFromFirstProfile(UnitProfileGroups profileGroups)
    {
        ResetUnitStats();

        var profile = profileGroups.Groups.SelectMany(group => group.Profiles).FirstOrDefault();
        if (profile is null)
        {
            return;
        }

        if (_armyDataService is not null)
        {
            var move = _armyDataService.ReadMoveValue(profile);
            _unitMoveFirstCm = move.FirstCm;
            _unitMoveSecondCm = move.SecondCm;
            UnitMov = move.DisplayValue;
        }
        else
        {
            _unitMoveFirstCm = null;
            _unitMoveSecondCm = null;
            UnitMov = "-";
        }

        UnitCc = UnitProfile.StatText(profile.Cc);
        UnitBs = UnitProfile.StatText(profile.Bs);
        UnitPh = UnitProfile.StatText(profile.Ph);
        UnitWip = UnitProfile.StatText(profile.Wip);
        UnitArm = UnitProfile.StatText(profile.Arm);
        UnitBts = UnitProfile.StatText(profile.Bts);
        UnitS = UnitProfile.StatText(profile.S);

        // The viewer shows AVA as sent (including -1) and labels structure troopers STR.
        UnitAva = profile.Ava switch
        {
            null => "-",
            255 => "T",
            var ava => ava.Value.ToString()
        };
        UnitVitalityHeader = profile.Str ? "STR" : "VITA";
        UnitVitality = UnitProfile.StatText(profile.W);
    }

    private static string NormalizeTokenText(string value)
    {
        var lowered = value.ToLowerInvariant();
        var sb = new StringBuilder(lowered.Length);
        foreach (var c in lowered)
        {
            if (char.IsLetterOrDigit(c) || c == '.')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append(' ');
            }
        }

        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }

    private void UpdateUnitMoveDisplay()
    {
        UnitMov = _armyDataService?.FormatMoveValue(_unitMoveFirstCm, _unitMoveSecondCm) ?? "-";
    }

}
