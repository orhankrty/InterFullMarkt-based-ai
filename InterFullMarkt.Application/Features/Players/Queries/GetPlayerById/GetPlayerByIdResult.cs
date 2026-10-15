namespace InterFullMarkt.Application.Features.Players.Queries.GetPlayerById;

/// <summary>
/// Result of fetching a player by ID
/// </summary>
public sealed class GetPlayerByIdResult
{
    public Guid PlayerId { get; set; }
    public Guid Id => PlayerId;
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public string NationalityFlag { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    
    /// <summary>
    /// Age calculated from DateOfBirth
    /// </summary>
    public int Age { get; set; }
    
    public DateTime DateOfBirth { get; set; }
    public int Height { get; set; }
    public decimal Weight { get; set; }
    public string PreferredFoot { get; set; } = string.Empty;
    
    public string MarketValue { get; set; } = string.Empty;
    public string MarketValueCurrency { get; set; } = string.Empty;
    public decimal MarketValueAmount { get; set; }
    
    public int? JerseyNumber { get; set; }
    
    public string? ClubName { get; set; }
    public Guid? ClubId { get; set; }
    
    public DateTime CreatedDate { get; set; }
    
    /// <summary>
    /// Market value history for Chart.js (simulated: last 12 months)
    /// </summary>
    public List<MarketValueHistoryPoint> MarketValueHistory { get; set; } = new();
    
    /// <summary>
    /// Transfer history
    /// </summary>
    public List<TransferHistoryPoint> TransferHistory { get; set; } = new();
    
    /// <summary>
    /// AI-predicted market value (12 months forward)
    /// </summary>
    public string? AiPredictedValue { get; set; }
    
    /// <summary>
    /// AI prediction change percentage
    /// </summary>
    public decimal AiPredictionChange { get; set; } = 0;
    
    /// <summary>
    /// AI prediction reasoning/explanation
    /// </summary>
    public string? AiPredictionReasoning { get; set; }
    
    /// <summary>
    /// Confidence level of AI prediction (Low, Medium, High)
    /// </summary>
    public string AiPredictionConfidence { get; set; } = "Medium";

    // 🤖 AI Scouting Insights
    public string? AiScoutingCategory { get; set; }
    public List<string> AiPros { get; set; } = new();
    public List<string> AiSimilarPlayers { get; set; } = new();

    // 📊 Scout & Radar Metrikleri
    public int Pace { get; set; }
    public int Shooting { get; set; }
    public int Passing { get; set; }
    public int Dribbling { get; set; }
    public int Defending { get; set; }
    public int Physicality { get; set; }
    public int ScoutRating { get; set; }
    public bool IsWonderkid { get; set; }
    public string ScoutTier { get; set; } = string.Empty;
    public decimal PotentialGrowthPercentage { get; set; }
    public decimal PotentialGrowth => PotentialGrowthPercentage;
}

/// <summary>
/// Point on market value history chart
/// </summary>
public sealed class MarketValueHistoryPoint
{
    public string Month { get; set; } = string.Empty; // e.g., "Jan 2025"
    public decimal Value { get; set; }
    public string ValueFormatted { get; set; } = string.Empty; // e.g., "€42M"
}

/// <summary>
/// Transfer history entry
/// </summary>
public sealed class TransferHistoryPoint
{
    public DateTime TransferDate { get; set; }
    public string FromClub { get; set; } = string.Empty;
    public string ToClub { get; set; } = string.Empty;
    public string Fee { get; set; } = string.Empty;
    public string TransferType { get; set; } = string.Empty; // Free, Loan, Permanent, etc.
}
