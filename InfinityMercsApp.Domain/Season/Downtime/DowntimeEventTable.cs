using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.Domain.Season.Downtime;

public static class DowntimeEventTable
{
    public static DowntimeEvent LookupByRoll(int roll)
    {
        return roll switch
        {
            <= 2 => Event1To2,
            <= 4 => Event3To4,
            <= 6 => Event5To6,
            <= 8 => Event7To8,
            <= 10 => Event9To10,
            <= 12 => Event11To12,
            <= 14 => Event13To14,
            <= 16 => Event15To16,
            <= 18 => Event17To18,
            _ => Event19To20,
        };
    }

    // All incident events (excludes 19-20) for manual selection.
    public static IReadOnlyList<DowntimeEvent> AllEvents =>
    [
        Event1To2, Event3To4, Event5To6, Event7To8, Event9To10,
        Event11To12, Event13To14, Event15To16, Event17To18
    ];

    // Helpers ----------------------------------------------------------------

    private static DowntimeTrait T(TraitKind kind) => new(kind);
    private static DowntimeTrait Negated(TraitKind kind) => new(kind, Negated: true);
    private static DowntimeTrait Skill(string skill) => new(TraitKind.Skill, skill);
    private static DowntimeTrait Requirement(string requirement) => new(TraitKind.Requirement, requirement);

    // Events -----------------------------------------------------------------

    private static readonly DowntimeEvent Event1To2 = new()
    {
        RollRange = "1-2",
        RepresentativeRoll = 1,
        Description = "A cop pulls you over for driving erratically...",
        Choices =
        [
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Cc, -10),
                Description = "...you reach for the cop's gun",
                Traits = [T(TraitKind.Attack), T(TraitKind.Weapon), T(TraitKind.Chaotic), T(TraitKind.Xp)]
            },
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Ph),
                Description = "...you floor it and drive away",
                Traits = [T(TraitKind.Chaotic), T(TraitKind.Xp)]
            },
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Wip),
                Description = "...you try to talk your way out with money",
                Traits = [T(TraitKind.Lawful), T(TraitKind.P2P)]
            }
        ]
    };

    private static readonly DowntimeEvent Event3To4 = new()
    {
        RollRange = "3-4",
        RepresentativeRoll = 3,
        Description = "An ammo dealer supplies you with faulty ammo and refuses to refund you...",
        EventTraits = ["SWC"],
        Choices =
        [
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Bs),
                Description = "...you attack the dealer's crew",
                Traits = [T(TraitKind.Attack), T(TraitKind.Chaotic), T(TraitKind.Xp), T(TraitKind.Swc)]
            },
            new DowntimeChoice
            {
                Test = DowntimeTest.Ps10PlusArm,
                Description = "...you let him shoot you to prove your point",
                Traits = [T(TraitKind.Attack), T(TraitKind.Swc)]
            },
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Wip),
                Description = "...intimidate the dealer in giving you an honest refund",
                Traits = [T(TraitKind.Cr), T(TraitKind.Swc)]
            }
        ]
    };

    private static readonly DowntimeEvent Event5To6 = new()
    {
        RollRange = "5-6",
        RepresentativeRoll = 5,
        Description = "You catch one of your Mercs in romantic cahoots with a rival Merc team member after they met in the most recent contract...",
        EventTraits = ["MVP", "Opponent (Mutual)"],
        RequiredParticipant = ParticipantKind.Mvp,
        Opponent = OpponentKind.Mutual,
        Choices =
        [
            new DowntimeChoice
            {
                Test = DowntimeTest.Ps10,
                Description = "...do nothing and see what happens",
                Traits = [T(TraitKind.Xp), Negated(TraitKind.Cr)]
            },
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Wip),
                Description = "...convince them to stop seeing each other",
                Traits = [T(TraitKind.Cr)]
            },
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Ph),
                Description = "...knock some sense into them",
                Traits = [T(TraitKind.Xp), T(TraitKind.Attack)]
            }
        ]
    };

    private static readonly DowntimeEvent Event7To8 = new()
    {
        RollRange = "7-8",
        RepresentativeRoll = 7,
        Description = "You get wind that a Rival Mercs team is planning an ambush on your team after some unfinished business from the most recent contract...",
        EventTraits = ["Opponent"],
        Opponent = OpponentKind.Opponent,
        Choices =
        [
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Cc, -10),
                Description = "...hide in wait and counter ambush them",
                Traits = [T(TraitKind.Attack), T(TraitKind.Chaotic), T(TraitKind.Xp)]
            },
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Wip),
                Description = "...avoid the confrontation",
                Traits = [Skill("Stealth"), T(TraitKind.Attack)]
            },
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Wip),
                Description = "...report them to the cops",
                Traits = [T(TraitKind.Lawful)]
            }
        ]
    };

    private static readonly DowntimeEvent Event9To10 = new()
    {
        RollRange = "9-10",
        RepresentativeRoll = 9,
        Description = "Select a Merc that went unconscious this contract. A rival Mercs team has taken them hostage after the most recent contract...",
        EventTraits = ["Opponent"],
        RequiredParticipant = ParticipantKind.KnockedOut,
        Opponent = OpponentKind.Opponent,
        Choices =
        [
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Wip),
                Description = "...you negotiate a ransom",
                Traits = [T(TraitKind.P2P), T(TraitKind.Lawful), T(TraitKind.Xp)]
            },
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Bs),
                Description = "...you ambush them at the drop site",
                Traits = [T(TraitKind.Chaotic), T(TraitKind.Attack), T(TraitKind.Xp)]
            },
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Ph),
                Description = "...let the Merc escape on their own",
                Traits = [T(TraitKind.Xp), Negated(TraitKind.Cr)]
            }
        ]
    };

    private static readonly DowntimeEvent Event11To12 = new()
    {
        RollRange = "11-12",
        RepresentativeRoll = 11,
        Description = "One of your Mercs has decided he's better suited to be the captain...",
        Choices =
        [
            new DowntimeChoice
            {
                Test = DowntimeTest.Ps10,
                Description = "...allow the change of leadership",
                Traits = [Negated(TraitKind.Cr), T(TraitKind.Lt)]
            },
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Wip),
                Description = "...negotiate a new salary",
                Traits = [T(TraitKind.Captain), T(TraitKind.Xp), T(TraitKind.P2P)]
            },
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Cc, -10),
                Description = "...agni kai",
                Traits = [T(TraitKind.Captain), T(TraitKind.Attack), T(TraitKind.Chaotic)]
            }
        ]
    };

    private static readonly DowntimeEvent Event13To14 = new()
    {
        RollRange = "13-14",
        RepresentativeRoll = 13,
        Description = "The Merc with the highest renown must be selected for this event. They have run into legal trouble and must present themselves in court...",
        RequiredParticipant = ParticipantKind.Renowned,
        Choices =
        [
            new DowntimeChoice
            {
                Test = DowntimeTest.Ps10,
                Description = "...you settle the issue out of court",
                Traits = [T(TraitKind.P2P), T(TraitKind.Xp)]
            },
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Wip),
                Description = "...they defend themselves in court",
                Traits = [T(TraitKind.Cr), T(TraitKind.Lawful)]
            },
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Cc, -10),
                Description = "...they attempt to intimidate the opposing legal team",
                Traits = [T(TraitKind.Cr), T(TraitKind.Chaotic)]
            }
        ]
    };

    private static readonly DowntimeEvent Event15To16 = new()
    {
        RollRange = "15-16",
        RepresentativeRoll = 15,
        Description = "You left behind your work laptop at the last contract and the opposing Merc team took it to see what they could gain...",
        Choices =
        [
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Wip),
                Description = "...you ask nicely for it back with some incentive",
                Traits = [T(TraitKind.P2P), T(TraitKind.Swc), T(TraitKind.Lawful)]
            },
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Wip),
                Description = "...you hack the computer to shut it down",
                Traits = [T(TraitKind.Attack), T(TraitKind.Xp), Requirement("Hacker")]
            },
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Wip),
                Description = "...you counter hack whoever is on the other end",
                Traits = [T(TraitKind.Attack), T(TraitKind.Chaotic), T(TraitKind.Xp), Requirement("Trinity Program")]
            }
        ]
    };

    private static readonly DowntimeEvent Event17To18 = new()
    {
        RollRange = "17-18",
        RepresentativeRoll = 17,
        Description = "Your team gets caught into a brawl after some heated discussion turns to fist to cuffs...",
        Choices =
        [
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Ph),
                Description = "...you intimidate them to stop fighting",
                Traits = [T(TraitKind.Attack), T(TraitKind.Lawful)]
            },
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Cc, -10),
                Description = "...you revel in the chaos",
                Traits = [T(TraitKind.Chaotic), Skill("Natural Born Warrior"), T(TraitKind.Attack)]
            },
            new DowntimeChoice
            {
                Test = DowntimeTest.For(Stat.Bs),
                Description = "...you bring a gun to a fist fight",
                Traits = [T(TraitKind.Chaotic), T(TraitKind.Cr)]
            }
        ]
    };

    private static readonly DowntimeEvent Event19To20 = new()
    {
        RollRange = "19-20",
        Description = "No incident occurs. You may reroll until you get an incident if you wish or choose to have no event.",
        IsNoIncident = true
    };
}
