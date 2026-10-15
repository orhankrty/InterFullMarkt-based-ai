namespace InterFullMarkt.Tests.Application.Services;

using Microsoft.Extensions.Configuration;
using InterFullMarkt.Application.Services;
using InterFullMarkt.Domain.Entities;
using InterFullMarkt.Domain.Enums;
using InterFullMarkt.Domain.ValueObjects;

/// <summary>
/// Unit tests for AIPricePredictionService
/// </summary>
public class AIPricePredictionServiceTests
{
    private readonly AIPricePredictionService _service;

    public AIPricePredictionServiceTests()
    {
        var config = new ConfigurationBuilder().Build();
        var httpClient = new HttpClient();
        _service = new AIPricePredictionService(httpClient, config);
    }

    [Fact]
    public async Task GenerateAIAnalysisAsync_ForYoungTalent_PredictsPriceIncrease()
    {
        // Arrange
        var player = CreateTestPlayer(
            age: 20,
            position: PlayerPosition.CM,
            currentValue: 40_000_000
        );

        // Act
        var (prediction, scouting) = await _service.GenerateAIAnalysisAsync(player, leagueCoefficient: 8.5m);

        // Assert
        Assert.NotNull(prediction);
        Assert.NotNull(scouting);
        Assert.True(prediction.ChangePercentage > 0, "Young talent should have positive growth");
        Assert.Equal("High", prediction.Confidence);
        Assert.NotEmpty(scouting.Category);
    }

    [Fact]
    public async Task GenerateAIAnalysisAsync_ForVeteranPlayer_PredictsPriceDecrease()
    {
        // Arrange
        var player = CreateTestPlayer(
            age: 35,
            position: PlayerPosition.CB,
            currentValue: 30_000_000
        );

        // Act
        var (prediction, scouting) = await _service.GenerateAIAnalysisAsync(player, leagueCoefficient: 7.0m);

        // Assert
        Assert.NotNull(prediction);
        Assert.NotNull(scouting);
        Assert.True(prediction.ChangePercentage < 0, "Veteran player should have negative growth");
    }

    [Fact]
    public async Task GenerateAIAnalysisAsync_StrikerPositionGetsBonus_ComparedToGoalkeeper()
    {
        // Arrange
        var strikerPlayer = CreateTestPlayer(age: 25, position: PlayerPosition.ST, currentValue: 50_000_000);
        var goalkeeperPlayer = CreateTestPlayer(age: 25, position: PlayerPosition.GK, currentValue: 50_000_000);

        // Act
        var (strikerPred, _) = await _service.GenerateAIAnalysisAsync(strikerPlayer, 8.0m);
        var (gkPred, _) = await _service.GenerateAIAnalysisAsync(goalkeeperPlayer, 8.0m);

        // Assert
        Assert.True(strikerPred.PredictedValue > gkPred.PredictedValue,
            "Strikers should be predicted higher than goalkeepers with same base value");
    }

    private Player CreateTestPlayer(int age, PlayerPosition position, decimal currentValue)
    {
        var dateOfBirth = DateTime.Now.AddYears(-age);
        var player = new Player(
            fullName: $"Test Player {position}",
            position: position,
            nationality: Nationality.CreateByCode("ES"),
            dateOfBirth: dateOfBirth,
            height: 185,
            weight: 82
        )
        {
            PreferredFoot = "Right"
        };

        player.UpdateMarketValue(Money.Create(currentValue, "EUR"));
        return player;
    }
}
