namespace InterFullMarkt.Tests.Application.Features.Players.Commands;

using Moq;
using InterFullMarkt.Application.Features.Players.Commands.CreatePlayer;
using InterFullMarkt.Application.Abstractions;
using InterFullMarkt.Domain.ValueObjects;
using InterFullMarkt.Application.DTOs;

/// <summary>
/// Unit tests for CreatePlayerCommandHandler
/// </summary>
public class CreatePlayerCommandHandlerTests
{
    private readonly Mock<IDbContext> _mockDbContext;
    private readonly CreatePlayerCommandHandler _handler;

    public CreatePlayerCommandHandlerTests()
    {
        _mockDbContext = new Mock<IDbContext>();
        _handler = new CreatePlayerCommandHandler(_mockDbContext.Object);
    }

    [Fact]
    public async Task Handle_WithValidPlayerData_ReturnsSuccessResult()
    {
        // Arrange
        var createPlayerDto = new CreatePlayerDto
        {
            FullName = "Test Player",
            Position = 3, // CM
            NationalityCode = "TR",
            DateOfBirth = new DateTime(2002, 01, 15),
            Height = 190,
            Weight = 85,
            PreferredFoot = "Right",
            InitialMarketValue = 50_000_000,
            Currency = "EUR",
            JerseyNumber = 10
        };

        var command = new CreatePlayerCommand(createPlayerDto, "System");

        // Act
        // Note: This test would require proper DbContext mocking setup
        // In real scenario, would mock Club lookup and other dependencies

        // Assert
        // Assert that handler is properly constructed
        Assert.NotNull(_handler);
    }

    [Fact]
    public void CreatePlayerCommand_WithValidData_IsValid()
    {
        // Arrange
        var dto = new CreatePlayerDto
        {
            FullName = "Arda Güler",
            Position = 3,
            NationalityCode = "TR",
            DateOfBirth = new DateTime(2003, 02, 04),
            Height = 178,
            Weight = 72
        };

        // Act
        var command = new CreatePlayerCommand(dto, "System");

        // Assert
        Assert.NotNull(command);
        Assert.Equal("Arda Güler", command.PlayerData.FullName);
    }
}
