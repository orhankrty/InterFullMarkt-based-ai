namespace InterFullMarkt.Application.Mappings;

using AutoMapper;
using InterFullMarkt.Domain.Entities;
using InterFullMarkt.Domain.Enums;
using InterFullMarkt.Application.DTOs;

/// <summary>
/// AutoMapper profili - Domain Entity'leri ile DTO'lar arasındaki dönüşümleri tanımlar.
/// </summary>
public sealed class PlayerProfile : Profile
{
    public PlayerProfile()
    {
        // Player Entity → PlayerDto
        CreateMap<Player, PlayerDto>()
            .ForMember(dest => dest.Position,
                opt => opt.MapFrom(src => src.Position.ToString()))
            .ForMember(dest => dest.Nationality,
                opt => opt.MapFrom(src => src.Nationality != null! ? $"{src.Nationality.FlagEmoji} {src.Nationality.CountryName}" : null!))
            .ForMember(dest => dest.Age,
                opt => opt.MapFrom(src => src.GetAge()))
            .ForMember(dest => dest.MarketValue,
                opt => opt.MapFrom(src => src.MarketValue != null ? $"{src.MarketValue.Amount:N0} {src.MarketValue.Currency}" : null))
            .ForMember(dest => dest.CurrentClubName,
                opt => opt.MapFrom(src => src.CurrentClub != null ? src.CurrentClub.Name : null))
            .ForMember(dest => dest.ImageUrl,
                opt => opt.MapFrom(src => src.ImageUrl))
            .AfterMap((src, dest) =>
            {
                var metrics = InterFullMarkt.Application.Common.Helpers.ScoutCalculator.CalculateMetrics(
                    src.Position,
                    src.GetAge(),
                    src.Height,
                    src.Weight,
                    src.MarketValue?.Amount ?? 0);

                dest.Pace = metrics.Pace;
                dest.Shooting = metrics.Shooting;
                dest.Passing = metrics.Passing;
                dest.Dribbling = metrics.Dribbling;
                dest.Defending = metrics.Defending;
                dest.Physicality = metrics.Physicality;
                dest.ScoutRating = metrics.ScoutRating;
                dest.IsWonderkid = metrics.IsWonderkid;
                dest.ScoutTier = metrics.ScoutTier;
                dest.PotentialGrowth = metrics.PotentialGrowthPercentage;
            });

        // CreatePlayerDto → Player Entity
        // Not: Bu mapping'in Create Handler'da kullanılır
        // DTO'dan direct Entity oluşturulmaz, çünkü Entity'nin kendi kurucusu vardır
    }
}
