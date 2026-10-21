using System.Globalization;
using InfinityMercsApp.Infrastructure.API.UserDocuments;
using InfinityMercsApp.Services.Sync;

namespace InfinityMercsApp.Presentation.Sync;

/// <summary>How the user is asked which version of a conflicting file to keep.</summary>
/// <param name="KeepThisDevice">The answer that keeps this device's version.</param>
/// <param name="UseOtherDevice">The answer that takes the other device's version.</param>
public sealed record SyncConflictQuestion(string Question, string KeepThisDevice, string UseOtherDevice)
{
    public const string DecideLater = "Decide Later";

    public IReadOnlyList<string> Answers => [KeepThisDevice, UseOtherDevice];

    /// <summary>The choice <paramref name="answer"/> stands for; null for <see cref="DecideLater"/>.</summary>
    public SyncChoice? ChoiceFor(string? answer) =>
        answer == KeepThisDevice ? SyncChoice.KeepThisDevice
        : answer == UseOtherDevice ? SyncChoice.UseOtherDevice
        : null;

    public static SyncConflictQuestion For(SyncConflict conflict)
    {
        var file = $"{(conflict.Kind == UserDocumentKind.Company ? "The company" : "The season of")} \"{conflict.Name}\"";
        var when = conflict.OtherUpdatedAt.ToLocalTime().ToString("d MMM 'at' HH:mm", CultureInfo.CurrentCulture);
        if (conflict.OtherDeleted)
        {
            return new($"{file} was deleted on another device ({when}) but changed on this one. Keep it?",
                "Keep It", "Delete It");
        }

        if (conflict.DeletedHere)
        {
            return new($"{file} was deleted on this device but changed on another ({when}). Delete it everywhere?",
                "Delete It", "Keep the Other Device's");
        }

        return new($"{file} was changed on this device and on another ({when}). Which version do you want to keep?",
            "This Device's", "The Other Device's");
    }
}
