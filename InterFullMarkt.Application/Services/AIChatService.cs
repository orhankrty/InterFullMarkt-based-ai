namespace InterFullMarkt.Application.Services;

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

/// <summary>
/// AI-based Chatbot Service interacting with Google Gemini API
/// Provides conversational responses acting as a football expert and scout.
/// </summary>
public sealed class AIChatService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public AIChatService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GeminiAI:ApiKey"];
    }

    /// <summary>
    /// Sends a conversational message to Gemini and retrieves the response.
    /// </summary>
    public async Task<string> SendMessageAsync(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "YOUR_GEMINI_API_KEY_HERE")
        {
            return "Üzgünüm, şu anda API sistemim kapalı olduğu için size sadece simüle edilmiş (mock) bir yanıt verebiliyorum. Ben InterFullMarkt'ın yapay zeka asistanıyım!";
        }

        try
        {
            var systemPrompt = @"
Sen InterFullMarkt platformunun resmi Yapay Zeka Asistanısın (InterFullMarkt AI). 
Sen dünyanın en iyi futbol scout'u, transfer analisti ve stratejistisin. 
Kullanıcılara oyuncu değerleri, transfer olasılıkları, yetenek analizleri ve futbol dünyası hakkında samimi, profesyonel ve bilgilendirici cevaplar verirsin. 
Konuşmalarında Türkçe kullan.
";

            var prompt = $"{systemPrompt}\n\nKullanıcı: {userMessage}\nSen (InterFullMarkt AI):";

            var requestBody = new 
            { 
                contents = new[] 
                { 
                    new { parts = new[] { new { text = prompt } } } 
                },
                generationConfig = new { temperature = 0.5 } // A bit more creative for chat
            };

            var url = $"v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestBody);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                var textResult = doc.RootElement.GetProperty("candidates")[0]
                    .GetProperty("content").GetProperty("parts")[0]
                    .GetProperty("text").GetString();

                return textResult?.Trim() ?? "Yanıt alınamadı.";
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            return $"API Hatası ({response.StatusCode}): {errorBody}";
        }
        catch (Exception ex)
        {
            return $"Bağlantı hatası: {ex.ToString()}";
        }
    }
}
