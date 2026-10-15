namespace InterFullMarkt.Application.Features.Auth.Commands.Register;

using MediatR;
using Microsoft.EntityFrameworkCore;
using InterFullMarkt.Application.Abstractions;
using InterFullMarkt.Domain.Entities;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly IDbContext _dbContext;
    private readonly IAuthService _authService;

    public RegisterCommandHandler(IDbContext dbContext, IAuthService authService)
    {
        _dbContext = dbContext;
        _authService = authService;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
            return new RegisterResult { IsSuccess = false, ErrorMessage = "Bu e-posta adresi sistemde zaten kayıtlı." };

        var hash = _authService.HashPassword(request.Password);
        var user = new User(request.Username, request.Email, hash);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        return new RegisterResult { IsSuccess = true, UserId = user.Id };
    }
}
