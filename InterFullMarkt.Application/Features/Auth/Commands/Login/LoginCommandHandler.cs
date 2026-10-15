namespace InterFullMarkt.Application.Features.Auth.Commands.Login;

using MediatR;
using Microsoft.EntityFrameworkCore;
using InterFullMarkt.Application.Abstractions;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IDbContext _dbContext;
    private readonly IAuthService _authService;

    public LoginCommandHandler(IDbContext dbContext, IAuthService authService)
    {
        _dbContext = dbContext;
        _authService = authService;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        
        if (user == null || !_authService.VerifyPassword(request.Password, user.PasswordHash))
            return new LoginResult { IsSuccess = false, ErrorMessage = "Geçersiz e-posta veya şifre." };
            
        return new LoginResult { IsSuccess = true, UserId = user.Id, Username = user.Username, Role = user.Role };
    }
}
