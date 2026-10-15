namespace InterFullMarkt.Application.DTOs;

/// <summary>
/// Oyuncu bilgilerini okuma işlemleri için DTO.
/// Domain Entity'sinden transfer edilen salt veri taşır, hiçbir iş mantığı yoktur.
/// </summary>
public sealed class PlayerDto
{
    /// <summary>
    /// Oyuncu ID'si (Unique)
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Oyuncu adı ve soyadı
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// Pozisyon (GK, CB, CM, ST)
    /// </summary>
    public required string Position { get; set; }

    /// <summary>
    /// Oyuncu profil resmi URL'si
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Milliyeti (Ülke ve Bayrak)
    /// </summary>
    public required string Nationality { get; set; }

    /// <summary>
    /// Doğum tarihi
    /// </summary>
    public required DateTime DateOfBirth { get; set; }

    /// <summary>
    /// Yaşı (hesaplanmış)
    /// </summary>
    public required int Age { get; set; }

    /// <summary>
    /// Boy (cm)
    /// </summary>
    public required int Height { get; set; }

    /// <summary>
    /// Kilo (kg)
    /// </summary>
    public required decimal Weight { get; set; }

    /// <summary>
    /// Tercih edilen ayak
    /// </summary>
    public required string PreferredFoot { get; set; }

    /// <summary>
    /// Mevcut piyasa değeri
    /// </summary>
    public string? MarketValue { get; set; }

    /// <summary>
    /// Forma numarası
    /// </summary>
    public int? JerseyNumber { get; set; }

    /// <summary>
    /// Bulunduğu kulüp adı
    /// </summary>
    public string? CurrentClubName { get; set; }

    /// <summary>
    /// Oluşturulma tarihi
    /// </summary>
    public required DateTime CreatedDate { get; set; }

    /// <summary>
    /// Güncelleme tarihi
    /// </summary>
    public required DateTime UpdatedDate { get; set; }

    /// <summary>
    /// Wonderkid mi (21 yaş altı ve gelişim potansiyeli yüksek)
    /// </summary>
    public bool IsWonderkid { get; set; }

    /// <summary>
    /// Gelişim potansiyeli yüzdesi
    /// </summary>
    public decimal PotentialGrowth { get; set; }

    /// <summary>
    /// Scout Puanı (1-100)
    /// </summary>
    public int ScoutRating { get; set; }

    /// <summary>
    /// Scout Seviyesi
    /// </summary>
    public string ScoutTier { get; set; } = string.Empty;

    /// <summary>
    /// Radar Metrikleri
    /// </summary>
    public int Pace { get; set; }
    public int Shooting { get; set; }
    public int Passing { get; set; }
    public int Dribbling { get; set; }
    public int Defending { get; set; }
    public int Physicality { get; set; }
}
