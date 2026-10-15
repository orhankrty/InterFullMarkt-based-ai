using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using InterFullMarkt.Application.Abstractions;
using InterFullMarkt.Domain.Entities;

namespace InterFullMarkt.WebUI.Controllers;

public class AuthController : Controller
{
    private readonly IDbContext _dbContext;
    private readonly IAuthService _authService;

    public AuthController(IDbContext dbContext, IAuthService authService)
    {
        _dbContext = dbContext;
        _authService = authService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
            return RedirectToAction("Index", "Players");
            
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewData["Error"] = "Lütfen kullanıcı adı ve şifre giriniz.";
            return View();
        }

        // 1. Hızlı Demo Admin Girişi
        if (username == "admin" && password == "123456")
        {
            var adminUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (adminUser == null)
            {
                adminUser = new User(username, "admin@interfullmarkt.com", _authService.HashPassword("123456"), "Admin")
                {
                    FirstName = "System",
                    LastName = "Admin",
                    CreatedByUserId = "System"
                };
                _dbContext.Users.Add(adminUser);
                await _dbContext.SaveChangesAsync();
            }

            await SignInUserAsync(adminUser.Username, adminUser.Email, adminUser.Role);
            return RedirectToAction("Index", "Admin");
        }

        // 2. Veritabanındaki Kayıtlı Kullanıcı Kontrolü
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username || u.Email == username);
        if (user != null && _authService.VerifyPassword(password, user.PasswordHash))
        {
            await SignInUserAsync(user.Username, user.Email, user.Role);
            return RedirectToAction("Index", "Players");
        }

        ViewData["Error"] = "Kullanıcı adı veya şifre hatalı!";
        return View();
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
            return RedirectToAction("Index", "Players");

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string username, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(email))
        {
            ViewData["Error"] = "Lütfen tüm alanları doldurun!";
            return View();
        }

        if (await _dbContext.Users.AnyAsync(u => u.Username == username))
        {
            ViewData["Error"] = "Bu kullanıcı adı zaten alınmış.";
            return View();
        }

        if (await _dbContext.Users.AnyAsync(u => u.Email == email))
        {
            ViewData["Error"] = "Bu e-posta adresiyle zaten kayıt yapılmış.";
            return View();
        }

        var hashedPassword = _authService.HashPassword(password);
        var newUser = new User(username, email, hashedPassword, "User")
        {
            FirstName = username,
            LastName = "Üye",
            CreatedByUserId = "System"
        };
        _dbContext.Users.Add(newUser);
        await _dbContext.SaveChangesAsync();

        await SignInUserAsync(newUser.Username, newUser.Email, newUser.Role);

        TempData["Success"] = "Kayıt başarılı! InterFullMarkt'a hoş geldiniz.";
        return RedirectToAction("Index", "Players");
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Players");
    }

    private async Task SignInUserAsync(string username, string email, string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, 
            new ClaimsPrincipal(claimsIdentity));
    }
}