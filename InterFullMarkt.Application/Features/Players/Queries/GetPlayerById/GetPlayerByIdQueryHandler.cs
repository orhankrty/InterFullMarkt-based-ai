namespace InterFullMarkt.Application.Features.Players.Queries.GetPlayerById;

using MediatR;
using Microsoft.EntityFrameworkCore;
using InterFullMarkt.Application.Abstractions;
using InterFullMarkt.Application.Services;

/// <summary>
/// Handler for GetPlayerByIdQuery
/// Gets a player by ID with full details and simulated market history
/// </summary>
public sealed class GetPlayerByIdQueryHandler : IRequestHandler<GetPlayerByIdQuery, GetPlayerByIdResult>
{
    private readonly IDbContext _dbContext;
    private readonly AIPricePredictionService _aiService;

    public GetPlayerByIdQueryHandler(IDbContext dbContext, AIPricePredictionService aiService)
    {
        _dbContext = dbContext;
        _aiService = aiService;
    }

    public async Task<GetPlayerByIdResult> Handle(GetPlayerByIdQuery request, CancellationToken cancellationToken)
    {
        var player = await _dbContext.Players
            .Include(p => p.CurrentClub)
                .ThenInclude(c => c!.League)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PlayerId, cancellationToken);

        if (player == null)
            throw new KeyNotFoundException($"Oyuncu bulunamadı: {request.PlayerId}");

        var age = DateTime.Now.Year - player.DateOfBirth.Year;
        if (player.DateOfBirth.Date > DateTime.Now.AddYears(-age)) age--;

        var metrics = InterFullMarkt.Application.Common.Helpers.ScoutCalculator.CalculateMetrics(
            player.Position,
            age,
            player.Height,
            player.Weight,
            player.MarketValue?.Amount ?? 0);

        var result = new GetPlayerByIdResult
        {
            PlayerId = player.Id,
            FullName = player.FullName ?? string.Empty,
            Position = player.Position.ToString(),
            Nationality = player.Nationality?.CountryName ?? "Bilinmiyor",
            NationalityFlag = player.Nationality?.FlagEmoji ?? "🏳️",
            Age = age,
            DateOfBirth = player.DateOfBirth,
            Height = player.Height,
            Weight = player.Weight,
            PreferredFoot = player.PreferredFoot ?? "Belirtilmemiş",
            ImageUrl = player.ImageUrl,
            
            MarketValue = player.MarketValue is not null 
                ? $"€{(player.MarketValue.Amount / 1_000_000):F1}M" 
                : "Belirtilmemiş",
            MarketValueAmount = player.MarketValue?.Amount ?? 0,
            MarketValueCurrency = player.MarketValue?.Currency ?? "EUR",
            
            JerseyNumber = player.JerseyNumber,
            ClubName = player.CurrentClub?.Name,
            ClubId = player.CurrentClubId,
            
            CreatedDate = player.CreatedDate,
            
            // Scout & Radar Metrics
            Pace = metrics.Pace,
            Shooting = metrics.Shooting,
            Passing = metrics.Passing,
            Dribbling = metrics.Dribbling,
            Defending = metrics.Defending,
            Physicality = metrics.Physicality,
            ScoutRating = metrics.ScoutRating,
            IsWonderkid = metrics.IsWonderkid,
            ScoutTier = metrics.ScoutTier,
            PotentialGrowthPercentage = metrics.PotentialGrowthPercentage,

            // Generate simulated market history for last 12 months
            MarketValueHistory = GenerateMarketValueHistory(player.MarketValue?.Amount ?? 40_000_000),
            
            // Transfer history
            TransferHistory = new List<TransferHistoryPoint>()
        };

        // Transfer geçmişini veritabanından çek
        var dbTransfers = await _dbContext.Transfers
            .Where(t => t.PlayerId == player.Id)
            .OrderByDescending(t => t.TransferDate)
            .Take(10)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (dbTransfers.Count > 0)
        {
            var clubIds = dbTransfers.Select(t => t.FromClubId).Concat(dbTransfers.Select(t => t.ToClubId)).Distinct().ToList();
            var clubDict = await _dbContext.Clubs
                .Where(c => clubIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

            result.TransferHistory = dbTransfers.Select(t => new TransferHistoryPoint
            {
                TransferDate = t.TransferDate,
                FromClub = clubDict.TryGetValue(t.FromClubId, out var fc) ? fc : "Bilinmeyen Kulüp",
                ToClub = clubDict.TryGetValue(t.ToClubId, out var tc) ? tc : "Bilinmeyen Kulüp",
                Fee = t.Fee != null ? $"€{(t.Fee.Amount / 1_000_000):F1}M" : "Bedelsiz",
                TransferType = t.TransferType
            }).ToList();
        }

        // 🤖 Real AI Google Gemini Forecast & Scouting Analysis
        var leagueCoeff = player.CurrentClub?.League?.Coefficient ?? 8.0m;
        var (prediction, scoutingReport) = await _aiService.GenerateAIAnalysisAsync(player, leagueCoeff);

        result.AiPredictedValue = prediction.PredictedValueFormatted;
        result.AiPredictionChange = prediction.ChangePercentage;
        result.AiPredictionReasoning = prediction.Reasoning;
        result.AiPredictionConfidence = prediction.Confidence;

        result.AiScoutingCategory = scoutingReport.Category;
        result.AiPros = scoutingReport.Pros;
        result.AiSimilarPlayers = scoutingReport.SimilarArchetypes;

        return result;
    }

    /// <summary>
    /// Generate simulated market value history (last 12 months with realistic fluctuations)
    /// </summary>
    private static List<MarketValueHistoryPoint> GenerateMarketValueHistory(decimal currentValue)
    {
        var history = new List<MarketValueHistoryPoint>();
        var now = DateTime.UtcNow;
        var baseValue = currentValue * 0.85m; // Start from 85% of current value

        for (int i = 11; i >= 0; i--)
        {
            var date = now.AddMonths(-i);
            // Simulate gradual increase with some variance
            var variance = (decimal)(i * 0.015); // 1.5% per month
            var value = baseValue + (baseValue * variance);
            
            history.Add(new MarketValueHistoryPoint
            {
                Month = date.ToString("MMM yyyy"),
                Value = value,
                ValueFormatted = $"€{(value / 1_000_000):F1}M"
            });
        }

        return history;
    }

    /// <summary>
    /// AI Piyasa Değeri Tahmin Algoritması (Yapay Zeka Sinyali)
    /// Oyuncunun yaşı, potansiyeli ve mevcut kulüp büyüklüğüne göre gelecek yılki değerini tahmin eder.
    /// </summary>
    private static string CalculateAiForecast(Domain.Entities.Player player, decimal currentAmount)
    {
        var age = player.GetAge();
        decimal multiplier = 1.0m;

        // 🧠 Yaş ve Potansiyel Matrisi
        if (age <= 21) multiplier += 0.35m; // Wonderkid Peak
        else if (age > 21 && age <= 26) multiplier += 0.15m; // Gelişim Evresi
        else if (age > 26 && age <= 30) multiplier -= 0.05m; // Zirve / Düşüş Başlangıcı
        else multiplier -= 0.25m; // Kariyer Sonu

        var forecastedValue = currentAmount * multiplier;
        return $"€{(forecastedValue / 1_000_000):F1}M";
    }
}
