namespace InterFullMarkt.Application.Features.Auth.Commands.Login;

using MediatR;

public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public class LoginResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
