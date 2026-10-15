namespace InterFullMarkt.WebUI.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MediatR;
using FluentValidation;
using InterFullMarkt.Application.Features.Players.Commands.CreatePlayer;
using InterFullMarkt.Application.Features.Players.Queries.GetAllPlayers;
using InterFullMarkt.Application.Features.Players.Queries.GetPlayerById;
using InterFullMarkt.Application.DTOs;

/// <summary>
/// Oyuncu yönetimi controller'ı.
/// MediatR CQRS pattern'ını kullanarak Command ve Query'leri işler.
/// </summary>
[Route("Players")]
public sealed class PlayersController : Controller
{
    private readonly IMediator _mediator;
    private readonly ILogger<PlayersController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mediator">MediatR arayüzü</param>
    /// <param name="logger">Logger arayüzü</param>
    public PlayersController(IMediator mediator, ILogger<PlayersController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tüm oyuncuları listeler.
    /// </summary>
    /// <param name="page">Sayfa numarası (varsayılan: 1)</param>
    /// <param name="search">Arama terimi</param>
    /// <param name="sortBy">Sıralama kriteri</param>
    /// <param name="positions">Pozisyon filtresi (virgüllle ayrılmış)</param>
    /// <param name="nationalities">Milliyet filtresi (virgüllle ayrılmış)</param>
    /// <param name="minValue">Minimum piyasa değeri</param>
    /// <param name="maxValue">Maksimum piyasa değeri</param>
    /// <returns>Oyuncuların listelendiği görünüm</returns>
    [HttpGet]
    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index(int page = 1, string? search = null, string? sortBy = null,
        string? positions = null, string? nationalities = null, decimal? minValue = null, decimal? maxValue = null,
        bool? wonderkids = null, int? maxAge = null, string? foot = null)
    {
        try
        {
            _logger.LogInformation("Oyuncular sayfası istendi: Page={Page}, Search={Search}, Wonderkids={Wonderkids}", page, search, wonderkids);

            var query = new GetAllPlayersQuery
            {
                PageIndex = page - 1,
                PageSize = 12,
                SearchTerm = search,
                SortBy = sortBy ?? "name",
                Positions = positions,
                Nationalities = nationalities,
                MinMarketValue = minValue,
                MaxMarketValue = maxValue,
                OnlyWonderkids = wonderkids,
                MaxAge = maxAge,
                PreferredFoot = foot
            };

            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                ViewData["ErrorMessage"] = result.ErrorMessage ?? "Oyuncuları yükleme sırasında bir hata oluştu.";
                return View(result);
            }

            ViewData["CurrentPage"] = page;
            ViewData["SearchTerm"] = search;
            ViewData["SortBy"] = sortBy ?? "name";
            ViewData["Positions"] = positions;
            ViewData["Nationalities"] = nationalities;
            ViewData["MinValue"] = minValue;
            ViewData["MaxValue"] = maxValue;
            ViewData["Wonderkids"] = wonderkids;
            ViewData["MaxAge"] = maxAge;
            ViewData["Foot"] = foot;

            return View(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oyuncuları listeleyemedi");
            ViewData["ErrorMessage"] = "Sistemde bir hata oluştu: " + ex.Message;
            return View(new GetAllPlayersResult 
            { 
                Players = new List<PlayerDto>(), 
                Clubs = new List<ClubDto>(), 
                TotalCount = 0,
                PageIndex = 0,
                PageSize = 12
            });
        }
    }

    /// <summary>
    /// Yeni oyuncu oluşturma formunu gösterir.
    /// </summary>
    /// <returns>Yeni oyuncu oluşturma formu görünümü</returns>
    [HttpGet]
    [Route("Create")]
    public IActionResult Create()
    {
        return View(new CreatePlayerDto
        {
            FullName = string.Empty,
            Position = 1,
            NationalityCode = string.Empty,
            DateOfBirth = DateTime.Today.AddYears(-20),
            Height = 170,
            Weight = 70
        });
    }

    /// <summary>
    /// Yeni oyuncu oluşturur.
    /// </summary>
    /// <param name="createPlayerDto">Oyuncu verileri</param>
    /// <returns>İşlem sonucu görünümü</returns>
    [HttpPost]
    [Route("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePlayerDto createPlayerDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(createPlayerDto);
            }

            _logger.LogInformation("Yeni oyuncu oluşturma isteği: {FullName}", createPlayerDto.FullName);

            var command = new CreatePlayerCommand(createPlayerDto, User?.Identity?.Name ?? "System");
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Oyuncu oluşturulamadı.");
                return View(createPlayerDto);
            }

            TempData["Success"] = $"{createPlayerDto.FullName} başarıyla sisteme eklendi!";
            _logger.LogInformation("Oyuncu başarıyla oluşturuldu: {PlayerId}", result.PlayerId);

            return RedirectToAction("Index");
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validasyon hatası: {Message}", ex.Message);
            
            foreach (var error in ex.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return View(createPlayerDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oyuncu oluşturulurken hata: {Message}", ex.Message);
            ModelState.AddModelError(string.Empty, "Oyuncu oluşturulurken bir hata oluştu.");
            return View(createPlayerDto);
        }
    }

    /// <summary>
    /// Oyuncu detaylarını gösterir.
    /// </summary>
    /// <param name="id">Oyuncu benzersiz kimliği (GUID)</param>
    /// <returns>Oyuncu detay görünümü</returns>
    [HttpGet]
    [Route("Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        try
        {
            _logger.LogInformation("Oyuncu detayları istendi: {PlayerId}", id);

            var query = new GetPlayerByIdQuery(id);
            var result = await _mediator.Send(query);

            return View(result);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Oyuncu bulunamadı: {PlayerId}", id);
            TempData["Error"] = "Oyuncu bulunamadı.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oyuncu detaylarını getirirken hata");
            TempData["Error"] = "Oyuncu detaylarını yüklerken bir hata oluştu.";
            return RedirectToAction("Index");
        }
    }

    [HttpGet]
    [Route("Compare")]
    public async Task<IActionResult> Compare(Guid? id1, Guid? id2, Guid? id3)
    {
        var playerIds = new List<Guid>();
        if (id1.HasValue) playerIds.Add(id1.Value);
        if (id2.HasValue) playerIds.Add(id2.Value);
        if (id3.HasValue) playerIds.Add(id3.Value);

        if (playerIds.Count < 2)
        {
            TempData["Error"] = "Kıyaslama yapmak için en az 2 oyuncu seçmelisiniz.";
            return RedirectToAction("Index");
        }

        var results = new List<GetPlayerByIdResult>();
        foreach (var id in playerIds)
        {
            try
            {
                var result = await _mediator.Send(new GetPlayerByIdQuery(id));
                results.Add(result);
            }
            catch { /* Ignore missing or erroring players in comparison */ }
        }

        return View(results);
    }
}
