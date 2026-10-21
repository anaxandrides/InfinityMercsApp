using System.Text;
using InfinityMercsApp.Domain.Models.Army.Profiles;

namespace InfinityMercsApp.Infrastructure.Data;

/// <summary>
/// Canonical text for profile groups and options: two values give the same text exactly when every field
/// the domain model carries is the same. Used for change detection and de-duplication.
/// </summary>
internal static class ProfileFingerprint
{
    public static string Of(ProfileOption option)
    {
        var text = new StringBuilder();
        AppendOption(text, option);
        return text.ToString();
    }

    public static void AppendGroups(StringBuilder text, UnitProfileGroups groups)
    {
        foreach (var group in groups.Groups)
        {
            text.Append("G:").Append(group.Id).Append(',').Append(group.Isc).Append(',').Append(group.Category).Append(';');
            foreach (var profile in group.Profiles)
            {
                text.Append("P:").Append(profile.Id).Append(',').Append(profile.Name).Append(',').Append(profile.Logo)
                    .Append(',').Append(profile.Type).Append(',').Append(profile.Str).Append(',').Append(profile.StrValue)
                    .Append(',').Append(profile.Move).Append(',').Append(string.Join('/', profile.MoveCm))
                    .Append(',').Append(profile.Cc).Append(',').Append(profile.Bs).Append(',').Append(profile.Ph)
                    .Append(',').Append(profile.Wip).Append(',').Append(profile.Arm).Append(',').Append(profile.Bts)
                    .Append(',').Append(profile.W).Append(',').Append(profile.S).Append(',').Append(profile.Ava)
                    .Append(',').Append(profile.Vita).Append(',').Append(profile.HasStatFields).Append(';');
                foreach (var kind in Enum.GetValues<ProfileEntryKind>())
                {
                    AppendEntries(text, kind, profile.Entries(kind));
                }
            }

            foreach (var option in group.Options)
            {
                AppendOption(text, option);
            }
        }
    }

    private static void AppendOption(StringBuilder text, ProfileOption option)
    {
        text.Append("O:").Append(option.Id).Append(',').Append(option.Name).Append(',').Append(option.Swc)
            .Append(',').Append(option.Cost).Append(',').Append(option.Minis).Append(',').Append(option.Disabled).Append(';');
        foreach (var order in option.Orders)
        {
            text.Append("R:").Append(order.Type).Append(',').Append(order.List).Append(',').Append(order.Total).Append(';');
        }

        foreach (var include in option.Includes)
        {
            text.Append("I:").Append(include.GroupId).Append(',').Append(include.OptionId).Append(';');
        }

        foreach (var kind in Enum.GetValues<ProfileEntryKind>())
        {
            AppendEntries(text, kind, option.Entries(kind));
        }
    }

    private static void AppendEntries(StringBuilder text, ProfileEntryKind kind, IReadOnlyList<ProfileEntry> entries)
    {
        foreach (var entry in entries)
        {
            text.Append((int)kind).Append(':').Append(entry.Id).Append('x').Append(entry.Quantity)
                .Append('@').Append(entry.Order).Append('[').Append(string.Join('/', entry.Extras)).Append("];");
        }
    }
}
