using InfinityMercsApp.Domain.Leagues;
using InfinityMercsApp.Services.SignIn;

namespace InfinityMercsApp.Presentation.Leagues;

/// <summary>How the user is shown to a league's other members: the account's name, cut to fit.</summary>
public static class DisplayNames
{
    public const string Fallback = "A mercenary";

    public static string Of(SignedInAccount account)
    {
        var name = account.DisplayName.Trim();
        if (name.Length == 0)
        {
            return Fallback;
        }

        // An email address shows only its name part to other members.
        if (name.IndexOf('@') is > 0 and var at)
        {
            name = name[..at];
        }

        return name.Length <= LeagueLimits.DisplayNameLength ? name : name[..LeagueLimits.DisplayNameLength];
    }
}
