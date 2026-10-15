namespace InterFullMarkt.Application.Features.Transfers.Commands.ProcessTransfer;

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using InterFullMarkt.Application.Abstractions;
using InterFullMarkt.Domain.ValueObjects;

/// <summary>
/// Handler for ProcessTransferCommand
/// Validates and executes player transfer with all business rules
/// </summary>
public sealed class ProcessTransferCommandHandler : IRequestHandler<ProcessTransferCommand, ProcessTransferResult>
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<ProcessTransferCommandHandler> _logger;

    public ProcessTransferCommandHandler(IDbContext dbContext, ILogger<ProcessTransferCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ProcessTransferResult> Handle(ProcessTransferCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Transfer işleniliyor: Player={PlayerId}, From={FromClubId}, To={ToClubId}",
                request.PlayerId, request.FromClubId, request.ToClubId);

            // 1. Oyuncu kontrol et
            var player = await _dbContext.Players
                .Include(p => p.CurrentClub)
                .FirstOrDefaultAsync(p => p.Id == request.PlayerId, cancellationToken);

            if (player == null)
                return new ProcessTransferResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Oyuncu bulunamadı" 
                };

            // 2. Kaynak kulüp kontrol et
            var fromClub = await _dbContext.Clubs
                .FirstOrDefaultAsync(c => c.Id == request.FromClubId, cancellationToken);

            if (fromClub == null)
                return new ProcessTransferResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Kaynak kulüp bulunamadı" 
                };

            // 3. Hedef kulüp kontrol et
            var toClub = await _dbContext.Clubs
                .FirstOrDefaultAsync(c => c.Id == request.ToClubId, cancellationToken);

            if (toClub == null)
                return new ProcessTransferResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Hedef kulüp bulunamadı" 
                };

            // 4. Oyuncu kaynak kulübün kadrosunda mı?
            if (player.CurrentClubId != request.FromClubId)
                return new ProcessTransferResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = $"{player.FullName} {fromClub.Name}'da oynamıyor" 
                };

            // 5. Bütçe yeterliliği kontrol et
            var transferFee = Money.Create(request.TransferFee, request.Currency);
            if (toClub.Budget == null || toClub.Budget.Amount < transferFee.Amount)
                return new ProcessTransferResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = $"{toClub.Name} yetersiz bütçe: {(toClub.Budget?.Amount ?? 0m) / 1_000_000:F1}M € gerekli: {transferFee.Amount / 1_000_000:F1}M €" 
                };

            // 6. Hedef kulüp kadro sınırı kontrol et (max 23 oyuncu)
            var toClubPlayerCount = await _dbContext.Players
                .CountAsync(p => p.CurrentClubId == request.ToClubId, cancellationToken);

            if (toClubPlayerCount >= 23)
                return new ProcessTransferResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = $"{toClub.Name} kadro sınırına ulaştı (23/23)" 
                };

            // 7. Transfer oluştur
            var transfer = new InterFullMarkt.Domain.Entities.Transfer(
                fromClubId: request.FromClubId,
                toClubId: request.ToClubId,
                playerId: request.PlayerId,
                fee: transferFee,
                transferDate: DateTime.UtcNow,
                transferType: "Permanent"
            );

            // 8. Oyuncu transferini gerçekleştir
            player.CurrentClubId = request.ToClubId;

            // 9. Bütçeleri güncelle (Domain metotları ile)
            if (fromClub.Budget != null)
            {
                fromClub.AddToBudget(transferFee);
            }
            if (toClub.Budget != null)
            {
                toClub.DeductFromBudget(transferFee);
            }

            // 10. Transfer kayıt et
            _dbContext.Transfers.Add(transfer);

            // 11. Değişiklikleri kaydet
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Transfer başarılı: {PlayerName} {FromClub} → {ToClub}",
                player.FullName, fromClub.Name, toClub.Name);

            return new ProcessTransferResult
            {
                IsSuccess = true,
                TransferId = transfer.Id,
                PlayerId = player.Id,
                PlayerName = player.FullName ?? "Bilinmiyor",
                FromClubName = fromClub.Name,
                ToClubName = toClub.Name,
                TransferFee = $"€{(transferFee.Amount / 1_000_000):F1}M",
                TransferDate = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transfer işlenirken hata");
            return new ProcessTransferResult 
            { 
                IsSuccess = false, 
                ErrorMessage = $"Transfer hatası: {ex.Message}" 
            };
        }
    }
}
