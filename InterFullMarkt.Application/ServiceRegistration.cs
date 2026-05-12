namespace InterFullMarkt.Application;

using FluentValidation;
using MediatR;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using InterFullMarkt.Application.Common.Behaviors;
using InterFullMarkt.Application.Mappings;
using InterFullMarkt.Application.Features.Players.Commands.CreatePlayer;

/// <summary>
/// Application katmanının Dependency Injection ayarları.
/// MediatR, AutoMapper, FluentValidation ve Pipeline Behaviors'ı register eder.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    /// Application katmanını Service Container'a register eder.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // AutoMapper yapılandırması
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<PlayerProfile>();
            cfg.AddProfile<ClubProfile>();
        });

        // MediatR Konfigürasyonu
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ServiceRegistration).Assembly);
            
            // Pipeline Behaviors kaydı (Validasyon, Logging, vb.)
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // FluentValidation - Manual validator registration
        services.AddScoped<IValidator<CreatePlayerCommand>, CreatePlayerCommandValidator>();

        // AI Services & Http Client configuration for Gemini API
        services.AddHttpClient<InterFullMarkt.Application.Services.AIPricePredictionService>(client =>
        {
            client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
        });
        
        services.AddHttpClient<InterFullMarkt.Application.Services.AIChatService>(client =>
        {
            client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
        });

        return services;
    }
}
