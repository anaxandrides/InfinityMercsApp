namespace InfinityMercsApp.ViewModels.Components;

/// <summary>A trooper as a tile in a unit strip: name, logo, captain and rank badges, and selection.</summary>
public interface IUnitTile
{
    string Name { get; }

    string BaseUnitDisplayName { get; }

    /// <summary>The captain's tile sits on a raised plate.</summary>
    bool IsLieutenant { get; }

    bool IsSelected { get; }

    /// <summary>The selected tile's rail and outline.</summary>
    Color SelectionAccentColor { get; }

    /// <summary>The logos to try in order; the tile shows the first that loads.</summary>
    IReadOnlyList<string> LogoCandidates { get; }

    string CaptainIconPackagedPath { get; }

    string ExperienceIconPackagedPath { get; }
}
