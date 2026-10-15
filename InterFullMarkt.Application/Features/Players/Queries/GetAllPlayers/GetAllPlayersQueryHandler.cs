using InterFullMarkt.Domain.Enums;

namespace InterFullMarkt.Application.Features.Players.Queries.GetAllPlayers;

using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using InterFullMarkt.Application.DTOs;
using InterFullMarkt.Application.Abstractions;

/// <summary>
/// GetAllPlayersQuery'yi işleyen handler.
/// Veritabanından oyuncuları alır, DTO'ya dönüştürür.
/// </summary>
public sealed class GetAllPlayersQueryHandler : IRequestHandler<GetAllPlayersQuery, GetAllPlayersResult>
{
    private readonly IDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllPlayersQueryHandler> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="dbContext">Veritabanı</param>
    /// <param name="mapper">AutoMapper nesnesi</param>
    /// <param name="logger">Logger nesnesi</param>
    public GetAllPlayersQueryHandler(
        IDbContext dbContext,
        IMapper mapper,
        ILogger<GetAllPlayersQueryHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sorguyu işler
    /// </summary>
    /// <param name="request">Sorgu isteği</param>
    /// <param name="cancellationToken">İptal tokeni</param>
    /// <returns>Oyuncu listesi sonucu</returns>
    public async Task<GetAllPlayersResult> Handle(GetAllPlayersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Oyuncuları getiriliyor: PageIndex={PageIndex}, PageSize={PageSize}, SearchTerm={SearchTerm}, SortBy={SortBy}",
                request.PageIndex,
                request.PageSize,
                request.SearchTerm,
                request.SortBy);

            // 1. Base query
            var query = _dbContext.Players
                .Include(p => p.CurrentClub)
                .AsNoTracking();

            // 2. Arama filtresi (Oyuncu, kulüp veya mevki optimizasyonu)
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchLower = request.SearchTerm.Trim().ToLower();

                // Mevki kelimelerini PlayerPosition enum değerleriyle eşleştir
                var matchedPositions = new List<PlayerPosition>();
                if (searchLower.Contains("kaleci") || searchLower.Contains("goalkeeper") || searchLower == "gk")
                    matchedPositions.Add(PlayerPosition.GK);
                if (searchLower.Contains("defans") || searchLower.Contains("stoper") || searchLower.Contains("bek") || searchLower.Contains("defender") || searchLower == "cb" || searchLower == "df")
                    matchedPositions.Add(PlayerPosition.CB);
                if (searchLower.Contains("orta") || searchLower.Contains("midfield") || searchLower.Contains("kanat") || searchLower == "cm" || searchLower == "mf")
                    matchedPositions.Add(PlayerPosition.CM);
                if (searchLower.Contains("forvet") || searchLower.Contains("santrfor") || searchLower.Contains("striker") || searchLower == "st" || searchLower == "fw")
                    matchedPositions.Add(PlayerPosition.ST);

                query = query.Where(p =>
                    (p.FullName != null && p.FullName.ToLower().Contains(searchLower)) ||
                    (p.Nationality != null! && (p.Nationality.CountryName.ToLower().Contains(searchLower) || p.Nationality.CountryCode.ToLower().Contains(searchLower))) ||
                    (p.CurrentClub != null && (p.CurrentClub.Name.ToLower().Contains(searchLower) || (p.CurrentClub.ShortName != null && p.CurrentClub.ShortName.ToLower().Contains(searchLower)))) ||
                    (matchedPositions.Count > 0 && matchedPositions.Contains(p.Position)));
            }

            // 3. Pozisyon filtresi
            if (!string.IsNullOrWhiteSpace(request.Positions))
            {
                var positionList = request.Positions!.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim().ToLower())
                    .ToList();
                
                query = query.Where(p => positionList.Contains(p.Position.ToString().ToLower()));
            }

            // 4. Milliyet filtresi
            if (!string.IsNullOrWhiteSpace(request.Nationalities))
            {
                var nationalityList = request.Nationalities!.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => n.Trim().ToUpper())
                    .ToList();
                
                query = query.Where(p => p.Nationality != null && nationalityList.Contains(p.Nationality.CountryCode));
            }

            // 5. Piyasa değeri aralığı filtresi
            if (request.MinMarketValue.HasValue)
            {
                query = query.Where(p => p.MarketValue != null && p.MarketValue.Amount >= request.MinMarketValue.Value);
            }
            if (request.MaxMarketValue.HasValue)
            {
                query = query.Where(p => p.MarketValue != null && p.MarketValue.Amount <= request.MaxMarketValue.Value);
            }

            // 5.1 Wonderkid ve Yaş Filtresi
            if (request.OnlyWonderkids == true)
            {
                var wonderkidCutoff = DateTime.UtcNow.AddYears(-21);
                query = query.Where(p => p.DateOfBirth >= wonderkidCutoff);
            }
            else if (request.MaxAge.HasValue)
            {
                var ageCutoff = DateTime.UtcNow.AddYears(-request.MaxAge.Value);
                query = query.Where(p => p.DateOfBirth >= ageCutoff);
            }

            // 5.2 Ayak Filtresi
            if (!string.IsNullOrWhiteSpace(request.PreferredFoot))
            {
                var footLower = request.PreferredFoot.Trim().ToLower();
                query = query.Where(p => p.PreferredFoot.ToLower() == footLower);
            }

            // 6. Sıralama
            query = ApplySorting(query, request.SortBy, request.SortDirection);

            // 7. Kulüpleri ara (Eğer arama terimi varsa)
            var matchingClubs = new List<ClubDto>();
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchLower = request.SearchTerm.ToLower().Trim();
                var clubs = await _dbContext.Clubs
                    .Include(c => c.Players)
                    .Where(c => c.Name.ToLower().Contains(searchLower) || c.ShortName.ToLower().Contains(searchLower))
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
                
                matchingClubs = _mapper.Map<List<ClubDto>>(clubs);
            }

            // 8. Toplam sayı
            var totalCount = await query.CountAsync(cancellationToken);

            // 9. Sayfalama
            var players = await query
                .Skip(request.PageIndex * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // 10. DTO'ya dönüştür
            var playerDtos = _mapper.Map<List<PlayerDto>>(players);

            _logger.LogInformation(
                "Başarıyla {Count} oyuncu ve {ClubCount} kulüp getirildi (Toplam Oyuncu: {Total})",
                playerDtos.Count,
                matchingClubs.Count,
                totalCount);

            return new GetAllPlayersResult
            {
                TotalCount = totalCount,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
                Players = playerDtos,
                Clubs = matchingClubs,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oyuncuları getirirken hata oluştu");

            return new GetAllPlayersResult
            {
                TotalCount = 0,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
                Players = new List<PlayerDto>(),
                IsSuccess = false,
                ErrorMessage = $"Oyuncuları getirirken bir hata oluştu: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Query'ye sıralama kurallarını uygular.
    /// </summary>
    private static IQueryable<Domain.Entities.Player> ApplySorting(
        IQueryable<Domain.Entities.Player> query,
        string? sortBy,
        string? sortDirection)
    {
        var isDescending = sortDirection?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "age" => isDescending
                ? query.OrderByDescending(p => p.DateOfBirth)
                : query.OrderBy(p => p.DateOfBirth),

            "marketvalue" => isDescending
                ? query.OrderByDescending(p => p.MarketValue != null! ? p.MarketValue.Amount : 0m)
                : query.OrderBy(p => p.MarketValue != null! ? p.MarketValue.Amount : 0m),

            "position" => isDescending
                ? query.OrderByDescending(p => p.Position)
                : query.OrderBy(p => p.Position),

            "name" or _ => isDescending
                ? query.OrderByDescending(p => p.FullName)
                : query.OrderBy(p => p.FullName)
        };
    }
}
