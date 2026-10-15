namespace InterFullMarkt.Application.Features.Transfers.Events;

using MediatR;

/// <summary>
/// Transfer başarıyla gerçekleştiğinde fırlatılan Application/Domain Event.
/// AI Sinyalleri, Loglama ve Notification sistemlerini tetiklemek için Publish edilir.
/// </summary>
public sealed record TransferCompletedEvent(
    Guid TransferId, 
    Guid PlayerId, 
    Guid FromClubId, 
    Guid ToClubId, 
    decimal FeeAmount, 
    string Currency) : INotification;
