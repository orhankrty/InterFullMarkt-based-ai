namespace InterFullMarkt.Infrastructure.Data.Seeds;

using InterFullMarkt.Domain.Entities;
using InterFullMarkt.Domain.Enums;
using InterFullMarkt.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

public static class DbInitializer
{
    public static async Task SeedGalatasarayAsync(InterFullMarktDbContext context)
    {
        // 1. Önce mağaza ürünlerini tohumla (ürünler korunur)
        await SeedProductsAsync(context);

        // 2. Kullanıcı talebi doğrultusunda: Veritabanındaki tüm oyuncuları, kulüpleri ve bağlı kayıtları tamamen temizle
        var hasPlayers = await context.Players.IgnoreQueryFilters().AnyAsync();
        var hasClubs = await context.Clubs.IgnoreQueryFilters().AnyAsync();

        if (hasPlayers || hasClubs)
        {
            var transfers = await context.Transfers.ToListAsync();
            if (transfers.Any()) context.Transfers.RemoveRange(transfers);

            var players = await context.Players.IgnoreQueryFilters().ToListAsync();
            if (players.Any()) context.Players.RemoveRange(players);

            var clubs = await context.Clubs.IgnoreQueryFilters().ToListAsync();
            if (clubs.Any()) context.Clubs.RemoveRange(clubs);

            await context.SaveChangesAsync();
        }
    }

    private static Player CreatePlayer(
        string name, PlayerPosition pos, string country, DateTime dob,
        int height, int weight, string foot, int number, decimal value, Guid clubId,
        string? imageUrl = null)
    {
        // Fallback for missing or protected images
        if (string.IsNullOrEmpty(imageUrl) || imageUrl.Contains("transfermarkt.technology"))
        {
            imageUrl = $"https://eu.ui-avatars.com/api/?name={Uri.EscapeDataString(name)}&background=random&color=fff&size=512";
        }

        var player = new Player(name, pos, Nationality.CreateByCode(country), dob, height, weight)
        {
            ImageUrl = imageUrl,
            PreferredFoot = foot,
            JerseyNumber = number,
            CurrentClubId = clubId,
            CreatedByUserId = "System"
        };
        player.UpdateMarketValue(Money.Create(value, "EUR"));
        return player;
    }

    private static async Task SeedProductsAsync(InterFullMarktDbContext context)
    {
        if (await context.Products.AnyAsync())
            return;

        var products = new List<Product>
        {
            new Product(
                "Galatasaray 24/25 İç Saha Forması",
                "Kulübün ikonik parçalı tasarımıyla hazırlanan, en yeni teknoloji performans forması.",
                Money.Create(120, "EUR"),
                "Forma",
                "https://images.unsplash.com/photo-1543351611-58f69d7c1781?w=800&q=80",
                100) { IsFeatured = true, CreatedByUserId = "System" },

            new Product(
                "Elite Pro Krampon X",
                "Üst düzey kontrol ve hız için tasarlanmış profesyonel krampon.",
                Money.Create(249.99m, "EUR"),
                "Krampon",
                "https://images.unsplash.com/photo-1510566337590-2fc1f21d0faa?w=800&q=80",
                50) { IsFeatured = true, CreatedByUserId = "System" },

            new Product(
                "Şampiyonlar Ligi Resmi Maç Topu",
                "Resmi maç standartlarında, yüksek dayanıklılık ve mükemmel aerodinamik.",
                Money.Create(150, "EUR"),
                "Aksesuar",
                "https://images.unsplash.com/photo-1551958219-acbc608c6377?w=800&q=80",
                200) { IsFeatured = true, CreatedByUserId = "System" },
                
            new Product(
                "Antrenman Eşofman Takımı",
                "Hafif ve terletmeyen kumaş yapısıyla profesyonel antrenman takımı.",
                Money.Create(85, "EUR"),
                "Giyim",
                "https://images.unsplash.com/photo-1556816214-cb30ab0507a2?w=800&q=80",
                75) { CreatedByUserId = "System" },
                
            new Product(
                "Retro 2000 Koleksiyonu Tişört",
                "Galatasaray'ın efsanevi 2000 yılı başarısına ithaf edilen özel koleksiyon.",
                Money.Create(45, "EUR"),
                "Giyim",
                "https://images.unsplash.com/photo-1581655353564-df123a1eb820?w=800&q=80",
                120) { CreatedByUserId = "System" }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }
}