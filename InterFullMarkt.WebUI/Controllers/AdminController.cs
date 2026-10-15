using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediatR;
using InterFullMarkt.Application.Abstractions;
using InterFullMarkt.Application.Features.Clubs.Queries.GetAllClubs;
using InterFullMarkt.Application.Features.Players.Queries.GetAllPlayers;
using InterFullMarkt.Application.Features.Clubs.Commands.CreateClub;
using InterFullMarkt.Application.Features.Clubs.Commands.UpdateClub;
using InterFullMarkt.Application.Features.Clubs.Commands.DeleteClub;
using InterFullMarkt.Application.Features.Clubs.Queries.GetClubById;
using InterFullMarkt.Application.Features.Players.Commands.CreatePlayer;
using InterFullMarkt.Application.Features.Players.Commands.UpdatePlayer;
using InterFullMarkt.Application.Features.Players.Commands.DeletePlayer;
using InterFullMarkt.Application.Features.Players.Queries.GetPlayerById;
using InterFullMarkt.Application.Features.Leagues.Queries.GetAllLeagues;
using InterFullMarkt.Application.DTOs;
using InterFullMarkt.Domain.Entities;
using InterFullMarkt.Domain.Enums;
using InterFullMarkt.Domain.ValueObjects;

namespace InterFullMarkt.WebUI.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin")]
public class AdminController : Controller
{
    private readonly IMediator _mediator;
    private readonly IDbContext _dbContext;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IMediator mediator, IDbContext dbContext, ILogger<AdminController> logger)
    {
        _mediator = mediator;
        _dbContext = dbContext;
        _logger = logger;
    }

    // ==========================================
    // 1. DASHBOARD / GENEL BAKIŞ
    // ==========================================
    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index()
    {
        var clubCount = await _dbContext.Clubs.CountAsync();
        var playerCount = await _dbContext.Players.CountAsync();
        var orderCount = await _dbContext.Orders.CountAsync();
        var userCount = await _dbContext.Users.CountAsync();
        var productCount = await _dbContext.Products.CountAsync();

        // Toplam Piyasa Değeri (€)
        var totalMarketValue = await _dbContext.Players
            .Where(p => p.MarketValue != null)
            .SumAsync(p => p.MarketValue != null ? (decimal?)p.MarketValue.Amount : 0m) ?? 0m;

        // Toplam Mağaza Cirosu (€)
        var totalRevenue = await _dbContext.Orders
            .Where(o => o.TotalAmount != null && o.Status != "Cancelled")
            .SumAsync(o => o.TotalAmount != null ? (decimal?)o.TotalAmount.Amount : 0m) ?? 0m;

        ViewBag.ClubCount = clubCount;
        ViewBag.PlayerCount = playerCount;
        ViewBag.OrderCount = orderCount;
        ViewBag.UserCount = userCount;
        ViewBag.ProductCount = productCount;
        ViewBag.TotalMarketValue = totalMarketValue;
        ViewBag.TotalRevenue = totalRevenue;

        // Son 5 Sipariş
        ViewBag.RecentOrders = await _dbContext.Orders
            .Include(o => o.User)
            .OrderByDescending(o => o.OrderDate)
            .Take(5)
            .ToListAsync();

        // Son 5 Transfer
        ViewBag.RecentTransfers = await _dbContext.Transfers
            .OrderByDescending(t => t.TransferDate)
            .Take(5)
            .ToListAsync();

        // Kulüp ve Oyuncu isim haritası
        ViewBag.ClubNames = await _dbContext.Clubs.ToDictionaryAsync(c => c.Id, c => c.Name);
        ViewBag.PlayerNames = await _dbContext.Players.ToDictionaryAsync(p => p.Id, p => p.FullName);

        return View();
    }

    // ==========================================
    // 2. OYUNCU YÖNETİMİ (PLAYERS)
    // ==========================================
    [Route("Players")]
    public async Task<IActionResult> Players()
    {
        var result = await _mediator.Send(new GetAllPlayersQuery { PageSize = 1000 });
        return View(result.Players);
    }

    [HttpGet("Players/Create")]
    public async Task<IActionResult> CreatePlayer()
    {
        ViewBag.Clubs = await _mediator.Send(new GetAllClubsQuery());
        return View(new CreatePlayerDto
        {
            FullName = string.Empty,
            Position = 1,
            NationalityCode = "TR",
            DateOfBirth = DateTime.Today.AddYears(-20),
            Height = 180,
            Weight = 75,
            PreferredFoot = "Right",
            Currency = "EUR"
        });
    }

    [HttpPost("Players/Create")]
    public async Task<IActionResult> CreatePlayer(CreatePlayerDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Clubs = await _mediator.Send(new GetAllClubsQuery());
            return View(dto);
        }

        var command = new CreatePlayerCommand(dto, User.Identity?.Name ?? "Admin");
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Oyuncu oluşturulamadı.");
            ViewBag.Clubs = await _mediator.Send(new GetAllClubsQuery());
            return View(dto);
        }

        TempData["Success"] = $"{dto.FullName} başarıyla sisteme kaydedildi.";
        return RedirectToAction(nameof(Players));
    }

    [HttpGet("Players/Edit/{id}")]
    public async Task<IActionResult> EditPlayer(Guid id)
    {
        var player = await _dbContext.Players
            .Include(p => p.CurrentClub)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (player == null) return NotFound();

        ViewBag.Clubs = await _mediator.Send(new GetAllClubsQuery());

        var updateDto = new UpdatePlayerDto
        {
            Id = player.Id,
            FullName = player.FullName ?? string.Empty,
            Position = (int)player.Position,
            NationalityCode = player.Nationality?.CountryCode ?? "TR",
            DateOfBirth = player.DateOfBirth,
            Height = player.Height,
            Weight = (int)player.Weight,
            PreferredFoot = player.PreferredFoot,
            JerseyNumber = player.JerseyNumber,
            ImageUrl = player.ImageUrl,
            MarketValueAmount = player.MarketValue?.Amount ?? 0,
            Currency = player.MarketValue?.Currency ?? "EUR",
            CurrentClubId = player.CurrentClubId
        };

        return View(updateDto);
    }

    [HttpPost("Players/Edit/{id}")]
    public async Task<IActionResult> EditPlayer(UpdatePlayerDto dto)
    {
        await _mediator.Send(new UpdatePlayerCommand(dto, User.Identity?.Name ?? "Admin"));
        TempData["Success"] = "Oyuncu bilgileri başarıyla güncellendi.";
        return RedirectToAction(nameof(Players));
    }

    [HttpPost("Players/Delete/{id}")]
    public async Task<IActionResult> DeletePlayer(Guid id)
    {
        await _mediator.Send(new DeletePlayerCommand(id, User.Identity?.Name ?? "Admin"));
        return Json(new { success = true });
    }

    // ==========================================
    // 3. KULÜP YÖNETİMİ (TEAMS / CLUBS)
    // ==========================================
    [Route("Teams")]
    public async Task<IActionResult> Teams()
    {
        var clubs = await _mediator.Send(new GetAllClubsQuery());
        return View(clubs);
    }

    [HttpGet("Teams/Create")]
    public async Task<IActionResult> CreateTeam()
    {
        ViewBag.Leagues = await _mediator.Send(new GetAllLeaguesQuery());
        return View(new CreateClubDto());
    }

    [HttpPost("Teams/Create")]
    public async Task<IActionResult> CreateTeam(CreateClubDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Leagues = await _mediator.Send(new GetAllLeaguesQuery());
            return View(dto);
        }

        await _mediator.Send(new CreateClubCommand(dto, User.Identity?.Name ?? "Admin"));
        TempData["Success"] = $"{dto.Name} başarıyla eklendi.";
        return RedirectToAction(nameof(Teams));
    }

    [HttpGet("Teams/Edit/{id}")]
    public async Task<IActionResult> EditTeam(Guid id)
    {
        var club = await _mediator.Send(new GetClubByIdQuery(id));
        if (club == null) return NotFound();

        ViewBag.Leagues = await _mediator.Send(new GetAllLeaguesQuery());
        
        var updateDto = new UpdateClubDto
        {
            Id = club.Id,
            Name = club.Name,
            ShortName = club.ShortName,
            FoundingYear = club.FoundingYear,
            StadiumName = club.StadiumName,
            Colors = club.Colors,
            LogoUrl = club.LogoUrl,
            BudgetAmount = club.BudgetAmount ?? 0,
            Currency = "EUR",
            LeagueId = Guid.Empty
        };

        return View(updateDto);
    }

    [HttpPost("Teams/Edit/{id}")]
    public async Task<IActionResult> EditTeam(UpdateClubDto dto)
    {
        await _mediator.Send(new UpdateClubCommand(dto, User.Identity?.Name ?? "Admin"));
        TempData["Success"] = "Takım bilgileri başarıyla güncellendi.";
        return RedirectToAction(nameof(Teams));
    }

    [HttpPost("Teams/Delete/{id}")]
    public async Task<IActionResult> DeleteTeam(Guid id)
    {
        await _mediator.Send(new DeleteClubCommand(id, User.Identity?.Name ?? "Admin"));
        return Json(new { success = true });
    }

    // ==========================================
    // 4. SİPARİŞ YÖNETİMİ (ORDERS - YENİ)
    // ==========================================
    [HttpGet("Orders")]
    public async Task<IActionResult> Orders()
    {
        var orders = await _dbContext.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }

    [HttpGet("Orders/Detail/{id}")]
    public async Task<IActionResult> OrderDetail(Guid id)
    {
        var order = await _dbContext.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        return View(order);
    }

    [HttpPost("Orders/UpdateStatus/{id}")]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, string status)
    {
        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();

        order.Status = status;
        order.UpdatedByUserId = User.Identity?.Name ?? "Admin";
        await _dbContext.SaveChangesAsync();

        TempData["Success"] = $"Sipariş durumu '{status}' olarak güncellendi.";
        return RedirectToAction(nameof(OrderDetail), new { id });
    }

    // ==========================================
    // 5. MAĞAZA ÜRÜN YÖNETİMİ (PRODUCTS - YENİ)
    // ==========================================
    [HttpGet("Products")]
    public async Task<IActionResult> Products()
    {
        var products = await _dbContext.Products
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync();

        return View(products);
    }

    [HttpGet("Products/Create")]
    public IActionResult CreateProduct()
    {
        return View();
    }

    [HttpPost("Products/Create")]
    public async Task<IActionResult> CreateProduct(string name, string description, decimal price, string category, string? imageUrl, int stockQuantity, bool isFeatured)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(string.Empty, "Ürün adı zorunludur.");
            return View();
        }

        var product = new Product(name, description ?? string.Empty, Money.Create(price, "EUR"), category ?? "Forma", imageUrl, stockQuantity)
        {
            IsFeatured = isFeatured,
            CreatedByUserId = User.Identity?.Name ?? "Admin"
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        TempData["Success"] = $"{name} mağazaya başarıyla eklendi.";
        return RedirectToAction(nameof(Products));
    }

    [HttpGet("Products/Edit/{id}")]
    public async Task<IActionResult> EditProduct(Guid id)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        return View(product);
    }

    [HttpPost("Products/Edit/{id}")]
    public async Task<IActionResult> EditProduct(Guid id, string name, string description, decimal price, string category, string? imageUrl, int stockQuantity, bool isFeatured)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        product.Name = name;
        product.Description = description ?? string.Empty;
        product.Price = Money.Create(price, "EUR");
        product.Category = category;
        product.ImageUrl = imageUrl;
        product.StockQuantity = stockQuantity;
        product.IsFeatured = isFeatured;
        product.UpdatedByUserId = User.Identity?.Name ?? "Admin";

        await _dbContext.SaveChangesAsync();

        TempData["Success"] = "Ürün bilgileri başarıyla güncellendi.";
        return RedirectToAction(nameof(Products));
    }

    [HttpPost("Products/Delete/{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return Json(new { success = false, message = "Ürün bulunamadı." });

        product.IsDeleted = true;
        product.DeletedDate = DateTime.UtcNow;
        product.DeletedByUserId = User.Identity?.Name ?? "Admin";
        await _dbContext.SaveChangesAsync();

        return Json(new { success = true });
    }

    // ==========================================
    // 6. KULLANICI & ROL YÖNETİMİ (USERS - YENİ)
    // ==========================================
    [HttpGet("Users")]
    public async Task<IActionResult> Users()
    {
        var users = await _dbContext.Users
            .Include(u => u.Orders)
            .OrderByDescending(u => u.CreatedDate)
            .ToListAsync();

        return View(users);
    }

    [HttpPost("Users/ChangeRole/{id}")]
    public async Task<IActionResult> ChangeUserRole(Guid id, string role)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return Json(new { success = false, message = "Kullanıcı bulunamadı." });

        user.Role = role;
        user.UpdatedByUserId = User.Identity?.Name ?? "Admin";
        await _dbContext.SaveChangesAsync();

        return Json(new { success = true, role });
    }

    // ==========================================
    // 7. TRANSFER AUDIT / DENETİM (TRANSFERS - YENİ)
    // ==========================================
    [HttpGet("Transfers")]
    public async Task<IActionResult> Transfers()
    {
        var transfers = await _dbContext.Transfers
            .OrderByDescending(t => t.TransferDate)
            .ToListAsync();

        ViewBag.Clubs = await _dbContext.Clubs.ToDictionaryAsync(c => c.Id, c => c.Name);
        ViewBag.Players = await _dbContext.Players.ToDictionaryAsync(p => p.Id, p => p.FullName);

        return View(transfers);
    }
}
