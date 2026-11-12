namespace InfinityMercsApp.Api.Leagues;

/// <summary>What a write does to a document in a league's partition.</summary>
public enum DocumentWriteKind
{
    /// <summary>Adds the document; fails when one with its id is already kept.</summary>
    Create,

    /// <summary>Replaces the document; fails when it changed since it was read.</summary>
    Replace,

    /// <summary>Removes the document; fails when it changed since it was read.</summary>
    Delete
}
