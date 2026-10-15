namespace InterFullMarkt.Application.Features.Auth.Commands.Register;

using MediatR;

public record RegisterCommand(string Username, string Email, string Password) : IRequest<RegisterResult>;

public class RegisterResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid UserId { get; set; }
}
