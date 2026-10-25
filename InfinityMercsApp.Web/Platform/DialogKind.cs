namespace InfinityMercsApp.Web.Platform;

/// <summary>What a dialog asks for besides a button.</summary>
public enum DialogKind
{
    /// <summary>Only a button.</summary>
    Buttons,

    /// <summary>A line of text.</summary>
    Text,

    /// <summary>A whole number in a range.</summary>
    Number
}
