using InfinityMercsApp.Services.Sync;

namespace InfinityMercsApp.Presentation.Sync;

/// <summary>How the user is asked what to do with the companies and seasons on this device that aren't in their account.</summary>
public sealed record UnclaimedFilesQuestion(string Question)
{
    public const string AddToAccount = "Add Them to My Account";
    public const string KeepOnDevice = "Keep Them on This Device";
    public const string NotNow = "Not Now";

    public IReadOnlyList<string> Answers => [AddToAccount, KeepOnDevice];

    /// <summary>The choice <paramref name="answer"/> stands for; null for <see cref="NotNow"/>.</summary>
    public static ClaimChoice? ChoiceFor(string? answer) => answer switch
    {
        AddToAccount => ClaimChoice.AddToAccount,
        KeepOnDevice => ClaimChoice.KeepOnDevice,
        _ => null
    };

    public static UnclaimedFilesQuestion For(UnclaimedFiles files)
    {
        var parts = new List<string>();
        if (files.Companies > 0)
        {
            parts.Add($"{files.Companies} {(files.Companies == 1 ? "company" : "companies")}");
        }

        if (files.Seasons > 0)
        {
            parts.Add($"{files.Seasons} {(files.Seasons == 1 ? "season" : "seasons")}");
        }

        var isOne = files.Total == 1;
        return new($"This device has {string.Join(" and ", parts)} that {(isOne ? "isn't" : "aren't")} in your account. Add {(isOne ? "it" : "them")} to your account, so {(isOne ? "it's" : "they're")} kept online?");
    }
}
