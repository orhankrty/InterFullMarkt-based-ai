namespace InterFullMarkt.Application.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using InterFullMarkt.Application.Abstractions;
using InterFullMarkt.Application.DTOs;

/// <summary>
/// AI-based Chatbot Service interacting with Google Gemini API and local database context (RAG).
/// Provides multi-turn conversational responses acting as an elite football scout and transfer market analyst.
/// </summary>
public sealed class AIChatService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly IDbContext _dbContext;

    public AIChatService(HttpClient httpClient, IConfiguration configuration, IDbContext dbContext)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
        _apiKey = configuration["GeminiAI:ApiKey"];
        _model = string.IsNullOrWhiteSpace(configuration["GeminiAI:Model"]) 
            ? "gemini-1.5-flash" 
            : configuration["GeminiAI:Model"]!;
    }

    /// <summary>
    /// Çok turlu (multi-turn) geçmişi ve yerel veritabanı bağlamını destekleyen ana mesajlaşma metodu.
    /// </summary>
    public async Task<ChatResponseDto> SendMessageWithHistoryAsync(string userMessage, List<ChatHistoryItemDto>? history = null)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return new ChatResponseDto
            {
                Response = "Lütfen bir mesaj yazın.",
                IsLiveAi = false,
                Model = _model
            };
        }

        // 1. API Anahtarı Tanımlı Değilse: Akıllı Yerel Arama & Rehberlik Modu
        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "YOUR_GEMINI_API_KEY_HERE")
        {
            var localResponse = await HandleLocalFallbackAsync(userMessage);
            return new ChatResponseDto
            {
                Response = localResponse,
                IsLiveAi = false,
                Model = "Local Scout Engine (No API Key)"
            };
        }

        // 2. Canlı Google Gemini API Çağrısı (RAG Destekli)
        try
        {
            var systemInstructionText = await BuildSystemInstructionAsync();

            // Geçmiş mesajları Gemini formatına dönüştür
            var contentsList = new List<object>();

            if (history != null && history.Count > 0)
            {
                // Son 10 mesajı al (token limitini korumak için)
                var recentHistory = history.TakeLast(10);
                foreach (var item in recentHistory)
                {
                    if (string.IsNullOrWhiteSpace(item.Text)) continue;
                    var role = item.Role.Equals("model", StringComparison.OrdinalIgnoreCase) ? "model" : "user";
                    contentsList.Add(new
                    {
                        role = role,
                        parts = new[] { new { text = item.Text } }
                    });
                }
            }

            // Mevcut kullanıcı mesajını ekle
            contentsList.Add(new
            {
                role = "user",
                parts = new[] { new { text = userMessage } }
            });

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemInstructionText } }
                },
                contents = contentsList,
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 1200
                }
            };

            var url = $"v1beta/models/{_model}:generateContent?key={_apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestBody);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);

                if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0)
                {
                    var textResult = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    return new ChatResponseDto
                    {
                        Response = textResult?.Trim() ?? "Yanıt üretilemedi.",
                        IsLiveAi = true,
                        Model = _model
                    };
                }

                return new ChatResponseDto
                {
                    Response = "Gemini yanıt verdi ancak uygun içerik bulunamadı.",
                    IsLiveAi = false,
                    Model = _model
                };
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            return new ChatResponseDto
            {
                Response = $"⚠️ **Google Gemini API Hatası ({response.StatusCode})**:\n{errorBody}\n\n*Lütfen API anahtarınızın doğruluğunu ve internet bağlantınızı kontrol ediniz.*",
                IsLiveAi = false,
                Model = _model
            };
        }
        catch (Exception ex)
        {
            var fallback = await HandleLocalFallbackAsync(userMessage);
            return new ChatResponseDto
            {
                Response = $"⚠️ **Bağlantı Hatası:** Canlı yapay zekaya ulaşılamadı ({ex.Message}).\n\nVeritabanı bilgisiyle yanıtlandı:\n\n{fallback}",
                IsLiveAi = false,
                Model = "Local Fallback"
            };
        }
    }

    /// <summary>
    /// Geriye dönük uyumluluk için string döndüren tekil mesaj metodu.
    /// </summary>
    public async Task<string> SendMessageAsync(string userMessage)
    {
        var result = await SendMessageWithHistoryAsync(userMessage);
        return result.Response;
    }

    /// <summary>
    /// Veritabanındaki canlı oyuncu, kulüp ve ürün verilerini sistem promptuna enjekte eder (RAG).
    /// </summary>
    private async Task<string> BuildSystemInstructionAsync()
    {
        try
        {
            // En değerli 10 oyuncu
            var rawTopPlayers = await _dbContext.Players
                .AsNoTracking()
                .Include(p => p.CurrentClub)
                .OrderByDescending(p => p.MarketValue != null ? p.MarketValue.Amount : 0)
                .Take(10)
                .ToListAsync();

            var topPlayers = rawTopPlayers
                .Select(p => $"{p.FullName} ({p.Position}, {(p.CurrentClub != null ? p.CurrentClub.Name : "Kulüpsüz")}, {(p.MarketValue != null ? p.MarketValue.Amount : 0):N0} €)")
                .ToList();

            var topPlayersText = topPlayers.Count > 0 ? string.Join(", ", topPlayers) : "Bilgi bulunmuyor";

            // Kulüpler
            var clubs = await _dbContext.Clubs
                .AsNoTracking()
                .Take(10)
                .Select(c => c.Name)
                .ToListAsync();

            var clubsText = clubs.Count > 0 ? string.Join(", ", clubs) : "Bilgi bulunmuyor";

            // Mağaza ürünleri
            var products = await _dbContext.Products
                .AsNoTracking()
                .Take(8)
                .Select(pr => $"{pr.Name} ({pr.Price.Amount:N0} {pr.Price.Currency})")
                .ToListAsync();

            var productsText = products.Count > 0 ? string.Join(", ", products) : "Bilgi bulunmuyor";

            return $@"
Sen InterFullMarkt platformunun resmi Yapay Zeka Scout Asistanısın (InterFullMarkt AI).
Sen dünyanın en iyi futbol scout'u, transfer piyasası analisti ve taktik uzmanısın.

GÖREVLERİN:
1. Kullanıcılarla futbol, oyuncu yetenekleri, transfer dedikoduları ve taktikler üzerine samimi, profesyonel ve bilgilendirici Türkçe sohbet etmek.
2. InterFullMarkt platformu hakkında sorulan sorulara aşağıdaki güncel sistem verilerini kullanarak kesin ve doğru yanıtlar vermek.

GÜNCEL SİSTEM BİLGİLERİ (INTERFULLMARKT VERİTABANI):
- Sistemdeki En Değerli Oyuncular: {topPlayersText}
- Sistemdeki Öne Çıkan Kulüpler: {clubsText}
- Mağazadaki Bazı Ürünler: {productsText}

BİÇİMLENDİRME KURALLARI:
- Önemli futbolcu isimlerini, takımları ve parasal değerleri **kalın** (bold) yaz.
- Listelemelerde madde imi (*) veya numaralandırma kullan.
- Kullanıcıya her zaman saygılı, futbol tutkusu yüksek ve samimi bir scout diliyle hitap et.
";
        }
        catch
        {
            return @"
Sen InterFullMarkt platformunun resmi Yapay Zeka Scout Asistanısın (InterFullMarkt AI).
Sen futbol analisti ve transfer uzmanısın. Kullanıcılara futbol, transferler ve oyuncular hakkında samimi, profesyonel Türkçe bilgi verirsin.
";
        }
    }

    /// <summary>
    /// API anahtarı girilmediğinde veritabanından akıllı arama yapıp kullanıcıya net bilgi sunan yerel motor.
    /// </summary>
    private async Task<string> HandleLocalFallbackAsync(string userMessage)
    {
        var lower = userMessage.Trim().ToLowerInvariant();

        // 1. Oyuncu arama (İsim eşleşmesi)
        var players = await _dbContext.Players
            .AsNoTracking()
            .Include(p => p.CurrentClub)
            .ToListAsync();

        var matchedPlayer = players.FirstOrDefault(p => 
            !string.IsNullOrWhiteSpace(p.FullName) && 
            lower.Contains(p.FullName.ToLowerInvariant()));

        if (matchedPlayer == null)
        {
            // Kelime bazlı arama (Örn: "Arda", "Semih", "Osimhen")
            var words = lower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var w in words)
            {
                if (w.Length >= 3)
                {
                    matchedPlayer = players.FirstOrDefault(p => 
                        !string.IsNullOrWhiteSpace(p.FullName) && 
                        p.FullName.ToLowerInvariant().Contains(w));
                    if (matchedPlayer != null) break;
                }
            }
        }

        if (matchedPlayer != null)
        {
            var mv = matchedPlayer.MarketValue != null 
                ? $"{matchedPlayer.MarketValue.Amount:N0} {matchedPlayer.MarketValue.Currency}" 
                : "Belirtilmemiş";
            var club = matchedPlayer.CurrentClub != null ? matchedPlayer.CurrentClub.Name : "Kulüpsüz";

            return $"⚽ **{matchedPlayer.FullName}** ({matchedPlayer.Position})\n\n" +
                   $"* **Kulüp:** {club}\n" +
                   $"* **Piyasa Değeri:** {mv}\n" +
                   $"* **Doğum Tarihi / Yaş:** {matchedPlayer.DateOfBirth:dd.MM.yyyy} ({(DateTime.Now.Year - matchedPlayer.DateOfBirth.Year)} yaşında)\n" +
                   $"* **Fiziksel Özellikler:** {matchedPlayer.Height} cm / {matchedPlayer.Weight} kg\n" +
                   $"* **Tercih Ettiği Ayak:** {matchedPlayer.PreferredFoot}\n" +
                   $"* **Forma No:** #{matchedPlayer.JerseyNumber}\n\n" +
                   $"---\n" +
                   $"💡 *Not: Bu bilgi yerel veritabanımızdan sağlandı. Genel futbol analizleri ve serbest yapay zeka sohbeti için `appsettings.json` dosyasına Google Gemini API anahtarınızı tanımlayabilirsiniz.*";
        }

        // 2. En değerli oyuncular sorusu
        if (lower.Contains("değerli") || lower.Contains("pahalı") || lower.Contains("fiyat") || lower.Contains("liste"))
        {
            var topList = players
                .OrderByDescending(p => p.MarketValue?.Amount ?? 0)
                .Take(5)
                .ToList();

            if (topList.Count == 0)
            {
                return "🏆 **Sistemdeki Oyuncular:**\n\nŞu anda yerel veritabanında kayıtlı futbolcu bulunmuyor. Yönetim panelinden yeni futbolcular ekleyebilirsiniz.\n\n---\n💡 *Not: Google Gemini AI anahtarınızı `appsettings.json` dosyasına ekleyerek dünya futbolundaki tüm oyuncuları serbestçe sorgulayabilirsiniz!*";
            }

            var listText = string.Join("\n", topList.Select((p, idx) => 
                $"{idx + 1}. **{p.FullName}** ({p.CurrentClub?.Name ?? "Kulüpsüz"}) — **{p.MarketValue?.Amount:N0} €**"));

            return $"🏆 **InterFullMarkt'ın En Değerli Oyuncuları:**\n\n{listText}\n\n" +
                   $"---\n" +
                   $"💡 *Not: Bu liste yerel sistemimizden derlendi. Google Gemini AI ile derin taktik ve scout raporları almak için `appsettings.json` dosyasına Gemini API anahtarınızı ekleyebilirsiniz.*";
        }

        // 3. Kulüpler sorusu
        if (lower.Contains("kulüp") || lower.Contains("takım") || lower.Contains("lig"))
        {
            var clubs = await _dbContext.Clubs.AsNoTracking().Take(8).ToListAsync();

            if (clubs.Count == 0)
            {
                return "🏟️ **Kayıtlı Kulüpler:**\n\nŞu anda yerel veritabanında kayıtlı kulüp bulunmuyor. Yönetim panelinden kulüp ekleyebilirsiniz.\n\n---\n💡 *Not: Google Gemini AI anahtarınızı ekleyerek dünya genelindeki tüm takımları analiz edebilirsiniz.*";
            }

            var clubText = string.Join(", ", clubs.Select(c => $"**{c.Name}**"));

            return $"🏟️ **Sistemde Kayıtlı Bazı Kulüpler:**\n\n{clubText}\n\n" +
                   $"İstediğiniz bir oyuncunun adını yazarak detaylı profilini sorgulayabilirsiniz.";
        }

        // 4. Mağaza ürünleri sorusu
        if (lower.Contains("mağaza") || lower.Contains("ürün") || lower.Contains("forma") || lower.Contains("fiyat"))
        {
            var products = await _dbContext.Products.AsNoTracking().Take(5).ToListAsync();
            var prodText = string.Join("\n", products.Select(p => 
                $"* **{p.Name}** ({p.Category}) — **{p.Price.Amount:N0} {p.Price.Currency}**"));

            return $"🛍️ **Mağazamızdaki Bazı Popüler Ürünler:**\n\n{prodText}\n\n" +
                   $"Sipariş vermek için menüden **Mağaza** sekmesini ziyaret edebilirsiniz!";
        }

        // 5. Genel Karşılama ve Rehberlik
        return "Merhaba! Ben **InterFullMarkt AI Scout** asistanınızım. ⚽\n\n" +
               "Şu anda veritabanımız üzerinden oyuncu, kulüp ve mağaza sorgularınıza yanıt verebiliyorum:\n" +
               "* Bir oyuncunun adını yazın (Örn: *'Arda Güler'*, *'Semih Kılıçsoy'*)\n" +
               "* *'En değerli oyuncular'* veya *'Mağazadaki ürünler'* diye sorun.\n\n" +
               "💡 **Gerçek Yapay Zeka (Gemini AI) Aktivasyonu:**\n" +
               "Serbest dünya futbolu sohbeti, transfer tahminleri ve taktiksel tavsiyeler almak için `appsettings.json` dosyasına veya User Secrets'a ücretsiz **Google Gemini API Key** tanımlamanız yeterlidir!";
    }
}
