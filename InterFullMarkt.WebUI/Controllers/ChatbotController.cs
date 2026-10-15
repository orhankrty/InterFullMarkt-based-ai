namespace InterFullMarkt.WebUI.Controllers;

using System.Threading.Tasks;
using InterFullMarkt.Application.DTOs;
using InterFullMarkt.Application.Services;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request?.Message))
        {
            return BadRequest(new { Error = "Mesaj boş olamaz." });
        }

        var result = await _aiChatService.SendMessageWithHistoryAsync(request.Message, request.History);
        
        return Ok(new 
        { 
            Response = result.Response,
            IsLiveAi = result.IsLiveAi,
            Model = result.Model
        });
    }
}
