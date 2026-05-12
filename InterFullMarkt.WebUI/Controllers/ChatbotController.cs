namespace InterFullMarkt.WebUI.Controllers;

using InterFullMarkt.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
[Route("[controller]")]
public class ChatbotController : ControllerBase
{
    private readonly AIChatService _aiChatService;

    public ChatbotController(AIChatService aiChatService)
    {
        _aiChatService = aiChatService;
    }

    [HttpPost("SendMessage")]
    public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Message))
        {
            return BadRequest(new { Error = "Mesaj boş olamaz." });
        }

        var response = await _aiChatService.SendMessageAsync(request.Message);
        
        return Ok(new { Response = response });
    }
}

public class ChatMessageRequest
{
    public string Message { get; set; } = string.Empty;
}
