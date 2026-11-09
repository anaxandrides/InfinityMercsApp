using System.Text.Json;
using InfinityMercsApp.Api.Auth;
using InfinityMercsApp.Api.Http;
using InfinityMercsApp.Api.Leagues.Changes;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InfinityMercsApp.Api.Leagues;

/// <summary>What every league route shares: a valid sign-in token, bodies read within a small limit, and outcomes as status codes.</summary>
public sealed class LeagueEndpoints(IUserAuthenticator authenticator)
{
    /// <summary>The largest body accepted; league requests are small.</summary>
    public const int MaxBodyBytes = 16_384;

    /// <summary>Bodies must give every required and non-nullable property.</summary>
    public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web) { RespectNullableAnnotations = true };

    /// <summary>The signed-in user, or null when the request has no valid token.</summary>
    public Task<SignedInUser?> UserAsync(HttpRequest request, CancellationToken cancellationToken) =>
        authenticator.AuthenticateAsync(request, cancellationToken);

    /// <summary>The body as <typeparamref name="TBody"/> once it passes <paramref name="validator"/>; otherwise the reply to send.</summary>
    public static async Task<LeagueRequest<TBody>> ReadAsync<TBody>(HttpRequest request, IValidator<TBody> validator, CancellationToken cancellationToken)
        where TBody : class
    {
        if (await RequestBodies.ReadAsync(request, MaxBodyBytes, cancellationToken) is not { } body)
        {
            return LeagueRequest<TBody>.Refused(new StatusCodeResult(StatusCodes.Status413PayloadTooLarge));
        }

        TBody? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<TBody>(body, JsonOptions);
        }
        catch (JsonException)
        {
            parsed = null;
        }

        if (parsed is null)
        {
            return LeagueRequest<TBody>.Refused(new BadRequestObjectResult(new List<string> { "The body isn't what this route takes." }));
        }

        var problems = (await validator.ValidateAsync(parsed, cancellationToken)).Errors.Select(error => error.ErrorMessage).ToList();
        return problems.Count > 0
            ? LeagueRequest<TBody>.Refused(new BadRequestObjectResult(problems))
            : LeagueRequest<TBody>.Read(parsed);
    }

    /// <summary>The league as the user sees it on success, or the status code for how the change went.</summary>
    public static IActionResult Outcome(LeagueChangeResult result, string userId) => result switch
    {
        { Status: LeagueChangeStatus.Done, League: { } league } => Json(LeagueViews.For(league, userId)),
        { Status: LeagueChangeStatus.Forbidden } => new StatusCodeResult(StatusCodes.Status403Forbidden),
        { Status: LeagueChangeStatus.Rejected } => new BadRequestObjectResult(result.Problems),
        { Status: LeagueChangeStatus.Conflict } => new ConflictResult(),
        _ => new NotFoundResult()
    };

    public static JsonResult Json(object value) => new(value, JsonOptions);
}
