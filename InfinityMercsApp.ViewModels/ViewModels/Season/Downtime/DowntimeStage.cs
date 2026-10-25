namespace InfinityMercsApp.ViewModels.Season.Downtime;

/// <summary>Where the downtime event flow is: what the player is being asked for next.</summary>
public enum DowntimeStage
{
    /// <summary>Roll (or enter) the d20 that picks the event.</summary>
    AwaitingEventRoll,

    /// <summary>The roll was a quiet week: pick any event, or none.</summary>
    ChoosingEvent,

    /// <summary>Pick one of the event's choices.</summary>
    ChoosingChoice,

    /// <summary>Pick the merc who performs the choice.</summary>
    ChoosingParticipant,

    /// <summary>Decide how much CR to spend on a P2P test.</summary>
    SpendingCr,

    /// <summary>Roll (or enter) the d20 for the test.</summary>
    AwaitingTestRoll,

    /// <summary>The flow cannot go on: nobody is eligible, or the test has no target.</summary>
    Blocked,

    /// <summary>The event is settled and recorded.</summary>
    Resolved
}
