using Microsoft.AspNetCore.Mvc;

namespace InfinityMercsApp.Api.Leagues;

/// <summary>A request body as read: the body when it is acceptable, otherwise the reply refusing it.</summary>
public sealed record LeagueRequest<TBody>(TBody? Body, IActionResult? Refusal)
    where TBody : class
{
    public static LeagueRequest<TBody> Read(TBody body) => new(body, null);

    public static LeagueRequest<TBody> Refused(IActionResult refusal) => new(null, refusal);
}
