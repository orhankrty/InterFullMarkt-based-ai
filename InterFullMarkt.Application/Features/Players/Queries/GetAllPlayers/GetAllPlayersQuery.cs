namespace InterFullMarkt.Application.Features.Players.Queries.GetAllPlayers;

using MediatR;
using InterFullMarkt.Application.DTOs;

/// <summary>
/// Tüm oyuncuları getiren sorgu (MediatR Query).
/// Silinmiş oyuncuları filtreleyip, aktif oyuncuları döndürür.
/// </summary>
public sealed class GetAllPlayersQuery : IRequest<GetAllPlayersResult>
{
    /// <summary>
    /// Sayfalama başlangıç indeksi (0-tabanlı)
    /// </summary>
    public int PageIndex { get; set; } = 0;

    /// <summary>
    /// Sayfa başına oyuncu sayısı
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Arama terimi (opsiyonel)
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Sıralama kriteri (opsiyonel: "name", "age", "marketvalue")
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Pozisyon filtresi (virgüllle ayrılmış: "GK,CB,CM")
    /// </summary>
    public string? Positions { get; set; }

    /// <summary>
    /// Milliyet filtresi (virgüllle ayrılmış koddur: "TR,ES,GB")
    /// </summary>
    public string? Nationalities { get; set; }

    /// <summary>
    /// Minimum piyasa değeri
    /// </summary>
    public decimal? MinMarketValue { get; set; }

    /// <summary>
    /// Maksimum piyasa değeri
    /// </summary>
    public decimal? MaxMarketValue { get; set; }

    /// <summary>
    /// Sıralama yönü (opsiyonel: "asc", "desc")
    /// </summary>
    public string? SortDirection { get; set; } = "asc";

    /// <summary>
    /// Sadece Wonderkid (21 yaş altı) oyuncuları filtrele
    /// </summary>
    public bool? OnlyWonderkids { get; set; }

    /// <summary>
    /// Maksimum yaş sınırı
    /// </summary>
    public int? MaxAge { get; set; }

    /// <summary>
    /// Tercih edilen ayak filtresi (Left, Right)
    /// </summary>
    public string? PreferredFoot { get; set; }
}

/// <summary>
/// GetAllPlayersQuery'nin sonucu
/// </summary>
public sealed class GetAllPlayersResult
{
    /// <summary>
    /// Toplam oyuncu sayısı (sayfalamadan bağımsız)
    /// </summary>
    public required int TotalCount { get; set; }

    /// <summary>
    /// Mevcut sayfa indeksi
    /// </summary>
    public required int PageIndex { get; set; }

    /// <summary>
    /// Sayfa başına oyuncu sayısı
    /// </summary>
    public required int PageSize { get; set; }

    /// <summary>
    /// Toplam sayfa sayısı
    /// </summary>
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;

    /// <summary>
    /// Mevcut sayfadaki oyuncular
    /// </summary>
    public required List<PlayerDto> Players { get; set; } = new();

    /// <summary>
    /// Eşleşen kulüpler (Arama terimi varsa dolsun)
    /// </summary>
    public List<ClubDto> Clubs { get; set; } = new();

    /// <summary>
    /// Sonuçlara başarıyla ulaşılmış mı?
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// Hata mesajı (başarısız ise)
    /// </summary>
    public string? ErrorMessage { get; set; }
}
