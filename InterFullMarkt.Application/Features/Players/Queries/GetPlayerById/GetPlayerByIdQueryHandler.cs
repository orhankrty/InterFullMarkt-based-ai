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
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PlayerId, cancellationToken);

        if (player == null)
            throw new KeyNotFoundException($"Oyuncu bulunamadı: {request.PlayerId}");

        var age = DateTime.Now.Year - player.DateOfBirth.Year;
        if (player.DateOfBirth.Date > DateTime.Now.AddYears(-age)) age--;

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
            
            // Generate simulated market history for last 12 months
            MarketValueHistory = GenerateMarketValueHistory(player.MarketValue?.Amount ?? 40_000_000),
            
            // Transfer history (currently empty, will be extended when Transfer query implemented)
            TransferHistory = new List<TransferHistoryPoint>()
        };

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

        // 🤖 AI Forecast Calculation (UI'da göstermek üzere DTO'ya eklenebilir)
        // İpucu: GetPlayerByIdResult sınıfına 'AiForecastFormatted' özelliğini ekleyerek bunu View'e aktarabilirsiniz.
        // var aiForecast = CalculateAiForecast(player, result.MarketValueAmount);
        // result.AiForecastFormatted = aiForecast;

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
