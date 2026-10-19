namespace InfinityMercsApp.Domain.Models.Season;

/// <summary>
/// The renown each side deployed for a contract, and the inducements the company got and spent; what they bought
/// lasts for that contract only.
/// </summary>
public class ContractInducements
{
    /// <summary>The renown of the company's deployed troopers.</summary>
    public int DeployedRenown { get; set; }

    /// <summary>The renown the opponent deployed, as the opponent gave it.</summary>
    public int OpponentRenown { get; set; }

    /// <summary>A cooperative contract, which gives no inducements.</summary>
    public bool IsCooperative { get; set; }

    /// <summary>The inducements the company got: none unless it is the underdog.</summary>
    public int Received { get; set; }

    public List<HiredTroop> Hired { get; set; } = [];

    public int CommandTokens { get; set; }

    public List<RentedGear> Rentals { get; set; } = [];
}
