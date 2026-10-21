namespace InfinityMercsApp.Domain.Season.Downtime;

/// <summary>Who must perform a downtime event; KnockedOut is a trooper that went unconscious in the contract.</summary>
public enum ParticipantKind { None, AnyMerc, Mvp, Captain, Renowned, KnockedOut }
