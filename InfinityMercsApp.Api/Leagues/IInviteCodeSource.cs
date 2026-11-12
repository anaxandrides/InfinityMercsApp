namespace InfinityMercsApp.Api.Leagues;

/// <summary>Makes new invite codes.</summary>
public interface IInviteCodeSource
{
    /// <summary>A code no one can guess; it may still clash with one already issued.</summary>
    string Next();
}
