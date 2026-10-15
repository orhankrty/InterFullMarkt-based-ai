namespace InterFullMarkt.Application.DTOs;

using System.Collections.Generic;

/// <summary>
/// Sohbet geçmişindeki bir mesaj öğesi (user veya model)
/// </summary>
public class ChatHistoryItemDto
{
    public string Role { get; set; } = "user"; // "user" or "model"
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Chatbot'a gönderilen istek modeli
/// </summary>
public class ChatMessageRequestDto
{
    public string Message { get; set; } = string.Empty;
    public List<ChatHistoryItemDto> History { get; set; } = new();
}

/// <summary>
/// Chatbot'tan dönen yanıt modeli
/// </summary>
public class ChatResponseDto
{
    public string Response { get; set; } = string.Empty;
    public bool IsLiveAi { get; set; } = false;
    public string Model { get; set; } = string.Empty;
}
