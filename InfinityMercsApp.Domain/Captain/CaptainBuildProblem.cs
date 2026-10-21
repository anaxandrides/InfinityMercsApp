namespace InfinityMercsApp.Domain.Captain;

/// <summary>One way a captain build breaks the rules, with a message to show the player.</summary>
public sealed record CaptainBuildProblem(CaptainBuildProblemKind Kind, string Message);
