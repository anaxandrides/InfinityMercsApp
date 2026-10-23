namespace InfinityMercsApp.ApiClient.Telemetry;

/// <summary>How sending a telemetry batch went.</summary>
public enum TelemetrySendResult
{
    /// <summary>The API kept it.</summary>
    Delivered,

    /// <summary>The API turned it down for good (it didn't pass the API's checks); sending it again won't help.</summary>
    Refused,

    /// <summary>The API couldn't be reached or failed; try again later.</summary>
    Unavailable
}
