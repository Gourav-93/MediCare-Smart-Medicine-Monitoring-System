using mediCare.DTOs;

namespace MediCare.Services.Interfaces;

public interface IAiChatService
{
    Task<ChatResponseDto> ProcessQueryAsync(int userId, string role, ChatRequestDto request);
}