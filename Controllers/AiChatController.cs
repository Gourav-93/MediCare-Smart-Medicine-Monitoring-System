using System.Security.Claims;
using mediCare.DTOs;
using MediCare.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize(Roles = "ADMIN,PATIENT,CAREGIVER")]
public class AiChatController : ControllerBase
{
    private readonly IAiChatService _aiChatService;

    public AiChatController(IAiChatService aiChatService)
    {
        _aiChatService = aiChatService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) && id > 0 ? id : 0;
    }

    private string GetCurrentRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }

    // POST: api/chat/ask
    [HttpPost("ask")]
    public async Task<IActionResult> AskAi([FromBody] ChatRequestDto request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Message cannot be empty." });
        }

        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            return Unauthorized();
        }

        var role = GetCurrentRole();

        var response = await _aiChatService.ProcessQueryAsync(userId, role, request);
        return Ok(response);
    }
}