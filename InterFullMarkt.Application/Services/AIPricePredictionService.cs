namespace InterFullMarkt.Application.Services;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using InterFullMarkt.Domain.Entities;
using Microsoft.Extensions.Configuration;

/// <summary>
/// AI-based price prediction engine for players (Powered by Google Gemini AI)
/// Features automatic fallback to local heuristic math logic if the API fails or if API key is not configured.
/// </summary>
public sealed class AIPricePredictionService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public AIPricePredictionService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GeminiAI:ApiKey"];
    }

    public class PredictionResult
    {
        public decimal PredictedValue { get; set; }
        public string PredictedValueFormatted { get; set; } = string.Empty;
        public decimal ChangeAmount { get; set; }
        public decimal ChangePercentage { get; set; }
        public string Confidence { get; set; } = "Medium"; // Low, Medium, High
        public string Reasoning { get; set; } = string.Empty;
    }

    public class ScoutingReport
    {
        public string Category { get; set; } = string.Empty;
        public List<string> Pros { get; set; } = new();
        public List<string> SimilarArchetypes { get; set; } = new();
    }

    private class GeminiAiResponse
    {
        [JsonPropertyName("PredictedValue")]
        public decimal PredictedValue { get; set; }
        [JsonPropertyName("Confidence")]
        public string Confidence { get; set; } = string.Empty;
        [JsonPropertyName("Reasoning")]
        public string Reasoning { get; set; } = string.Empty;
        [JsonPropertyName("Category")]
        public string Category { get; set; } = string.Empty;
        [JsonPropertyName("Pros")]
        public List<string> Pros { get; set; } = new();
        [JsonPropertyName("SimilarArchetypes")]
        public List<string> SimilarArchetypes { get; set; } = new();
    }

    /// <summary>
    /// Predict future market value and generate a scouting report dynamically using Google Gemini API.
    /// Falls back to Math logic if Key is invalid or rate limited.
    /// </summary>
    public async Task<(PredictionResult Prediction, ScoutingReport Scouting)> GenerateAIAnalysisAsync(Player player, decimal leagueCoefficient = 1.0m)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "YOUR_GEMINI_API_KEY_HERE")
        {
            return (PredictMarketValueFallback(player, leagueCoefficient), GenerateScoutingReportFallback(player));
        }

        try
        {
            var currentValue = player.MarketValue?.Amount ?? 40_000_000;
            var age = CalculateAge(player.DateOfBirth);
            var prompt = $@"
You are a top-tier professional football scout and market analyst. Analyze the following player and provide a JSON response evaluating their next year's market potential.
Return strictly valid JSON only, without markdown code blocks, using this exact schema:
{{
  ""PredictedValue"": 55000000, 
  ""Confidence"": ""High"",
  ""Reasoning"": ""Your string explanation of the financial forecast in Turkish language"",
  ""Category"": ""A clear Turkish category like 'Geleceğin Gol Makinesi' or 'Modern Savunmacı'"",
  ""Pros"": [""Pro 1 in Turkish"", ""Pro 2 in Turkish""],
  ""SimilarArchetypes"": [""Famous Player A"", ""Famous Player B""]
}}

Player Data:
- Name: {player.FullName}
- Age: {age}
- Position: {player.Position}
- Height: {player.Height}cm, Weight: {player.Weight}kg
- Current Value: €{currentValue}
- League Difficulty Coeff: {leagueCoefficient}
";

            var requestBody = new 
            { 
                contents = new[] 
                { 
                    new { parts = new[] { new { text = prompt } } } 
                },
                generationConfig = new { temperature = 0.3 }
            };

            var url = $"v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestBody);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                var textResult = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

                // Clean markdown syntax if AI returned ```json ... ```
                if (textResult!.StartsWith("```json")) {
                    textResult = textResult.Substring(7);
                    if (textResult.EndsWith("```")) textResult = textResult.Substring(0, textResult.Length - 3);
                }
                textResult = textResult.Trim();

                var aiData = JsonSerializer.Deserialize<GeminiAiResponse>(textResult);

                if (aiData != null)
                {
                    var changeAmount = aiData.PredictedValue - currentValue;
                    var changePercentage = (changeAmount / currentValue) * 100;

                    var prediction = new PredictionResult
                    {
                        PredictedValue = aiData.PredictedValue,
                        PredictedValueFormatted = $"€{(aiData.PredictedValue / 1_000_000):F1}M",
                        ChangeAmount = changeAmount,
                        ChangePercentage = changePercentage,
                        Confidence = aiData.Confidence,
                        // Append watermark so we know it's a real response
                        Reasoning = aiData.Reasoning + " 🤖 [Google Gemini AI]"
                    };

                    var scouting = new ScoutingReport
                    {
                        Category = aiData.Category,
                        Pros = aiData.Pros,
                        SimilarArchetypes = aiData.SimilarArchetypes
                    };

                    return (prediction, scouting);
                }
            }
        }
        catch (Exception)
        {
            // If the Gemini API fails, timeout, or parsing error, fallback to the mathematical models automatically.
        }

        return (PredictMarketValueFallback(player, leagueCoefficient), GenerateScoutingReportFallback(player));
    }

    /// <summary>
    /// Fallback Mathematical Methods
    /// </summary>
    private PredictionResult PredictMarketValueFallback(Player player, decimal leagueCoefficient = 1.0m)
    {
        var currentValue = player.MarketValue?.Amount ?? 40_000_000;
        var age = CalculateAge(player.DateOfBirth);
        var ageMultiplier = CalculateAgeMultiplier(age);
        var positionMultiplier = CalculatePositionMultiplier(player.Position);
        var normalizedLeagueCoeff = Math.Max(0.7m, Math.Min(1.2m, leagueCoefficient / 10m));
        var performanceMultiplier = CalculatePerformanceMultiplier(player);

        var predictedValue = currentValue * ageMultiplier * positionMultiplier * normalizedLeagueCoeff * performanceMultiplier;
        var changeAmount = predictedValue - currentValue;
        var changePercentage = (changeAmount / currentValue) * 100;
        var confidence = DetermineConfidence(age, player.Position);
        
        // Custom footnote for Mock algorithm to show difference
        var reasoning = GenerateReasoning(age, ageMultiplier, changePercentage, confidence) + " ⚠️ [Simüle Edilmiş (Mock) Sistem. API Key Eksik veya Limitte.]";

        return new PredictionResult
        {
            PredictedValue = predictedValue,
            PredictedValueFormatted = $"€{(predictedValue / 1_000_000):F1}M",
            ChangeAmount = changeAmount,
            ChangePercentage = changePercentage,
            Confidence = confidence,
            Reasoning = reasoning
        };
    }

    private ScoutingReport GenerateScoutingReportFallback(Player player)
    {
        var age = CalculateAge(player.DateOfBirth);
        var report = new ScoutingReport();
        
        report.Category = player.Position switch
        {
            InterFullMarkt.Domain.Enums.PlayerPosition.ST => age < 23 ? "Geleceğin Gol Makinesi" : "Bitirici Forvet",
            InterFullMarkt.Domain.Enums.PlayerPosition.GK => "Kale Muhafızı",
            InterFullMarkt.Domain.Enums.PlayerPosition.CB => player.Height > 188 ? "Hava Hakimiyeti Ustası" : "Modern Savunmacı",
            InterFullMarkt.Domain.Enums.PlayerPosition.CM => "Orta Saha Dinamosu",
            _ => "Çok Yönlü Oyuncu"
        };

        if (player.Height > 185) report.Pros.Add("Yüksek Hava Hakimiyeti");
        if (age < 22) report.Pros.Add("Yüksek Gelişim Potansiyeli");
        if (player.Position == InterFullMarkt.Domain.Enums.PlayerPosition.ST) report.Pros.Add("Ceza Sahası Bitiriciliği");
        if (player.Position == InterFullMarkt.Domain.Enums.PlayerPosition.GK) report.Pros.Add("Refleks ve Çeviklik");

        report.SimilarArchetypes = player.Position switch
        {
            InterFullMarkt.Domain.Enums.PlayerPosition.ST => new List<string> { "Icardi (Mock Variant)", "Haaland" },
            InterFullMarkt.Domain.Enums.PlayerPosition.GK => new List<string> { "Muslera", "Neuer" },
            InterFullMarkt.Domain.Enums.PlayerPosition.CB => new List<string> { "Sanchez", "Van Dijk" },
            _ => new List<string> { "Torreira", "De Bruyne" }
        };

        return report;
    }

    private static decimal CalculateAgeMultiplier(int age) => age switch { <= 19 => 1.22m, <= 22 => 1.15m, <= 25 => 1.08m, <= 28 => 1.0m, <= 30 => 0.95m, <= 32 => 0.88m, <= 34 => 0.75m, _ => 0.55m };
    private static decimal CalculatePositionMultiplier(InterFullMarkt.Domain.Enums.PlayerPosition position) => position switch { InterFullMarkt.Domain.Enums.PlayerPosition.ST => 1.18m, InterFullMarkt.Domain.Enums.PlayerPosition.CM => 1.12m, InterFullMarkt.Domain.Enums.PlayerPosition.CB => 1.08m, InterFullMarkt.Domain.Enums.PlayerPosition.GK => 0.92m, _ => 1.0m };
    private static decimal CalculatePerformanceMultiplier(Player player) { var mult = 1.0m; if (player.Height >= 185 && player.Height <= 195) mult += 0.02m; if (player.Weight >= 75 && player.Weight <= 95) mult += 0.02m; if (player.Height > 0 && player.Weight > 0) mult += 0.01m; return mult; }
    private static string DetermineConfidence(int age, InterFullMarkt.Domain.Enums.PlayerPosition position) => age >= 23 && age <= 28 ? "High" : age >= 19 && age < 23 ? "High" : age >= 29 && age <= 34 ? "Medium" : "Low";
    private static string GenerateReasoning(int age, decimal ageMultiplier, decimal changePercentage, string confidence) { var reason = age <= 21 ? "Genç yetenek, muazzam büyüme potansiyeli" : age <= 26 ? "Zirve yıllarına yaklaşan, hızla tırmanan değer" : age <= 29 ? "Yaşı ilerlese de rekabetçiliğini koruyor" : age <= 32 ? "Veteran dönem, değer kaybı süreci" : "Kariyer sonu, keskin değer azalışı bekleniyor"; if (changePercentage > 5) reason += ". Mükemmel piyasa momentumu."; else if (changePercentage > 0) reason += ". Ilımlı büyüme potansiyeli belirtisi."; return reason + $" (Güven: {confidence})"; }
    private static int CalculateAge(DateTime dateOfBirth) { var today = DateTime.UtcNow; var age = today.Year - dateOfBirth.Year; if (dateOfBirth.Date > today.AddYears(-age)) age--; return age; }
}
