using System.Text;
using System.Text.Json;
using mediCare.DTOs;
using MediCare.Data;
using MediCare.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MediCare.Services;

public class AiChatService : IAiChatService
{
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AiChatService> _logger;
    private readonly string _geminiApiKey;

    public AiChatService(
        AppDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AiChatService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _geminiApiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
    }

    public async Task<ChatResponseDto> ProcessQueryAsync(int userId, string role, ChatRequestDto request)
    {
        var rawQuery = request.Message?.Trim() ?? string.Empty;

        // 1. Minimal, Neutral System Instruction for General-Purpose AI
        const string systemInstruction = 
            "You are a helpful, general-purpose AI assistant. " +
            "Answer the user's questions naturally, clearly, and accurately. " +
            "Adapt your response to the user's request across any topic, including programming, writing, education, general knowledge, problem-solving, and everyday inquiries. " +
            "If the user asks about MediCare, you may explain that it is a smart medicine reminder and family monitoring web application designed to track prescriptions, send dose alerts, and keep caregivers informed.";

        // 2. Call AI Provider with Full Conversational Context
        var replyText = await CallGeminiApiAsync(systemInstruction, rawQuery, request.History);

        if (string.IsNullOrWhiteSpace(replyText))
        {
            _logger.LogWarning("[AiChatService] AI provider returned empty or was unreachable.");
            replyText = "I'm sorry, I am currently unable to reach the AI service. Please check your network connection or try again in a moment.";
        }

        return new ChatResponseDto
        {
            Reply = replyText,
            Timestamp = DateTime.Now,
            QuickSuggestions = new List<string>()
        };
    }

    private async Task<string> CallGeminiApiAsync(string systemPrompt, string userPrompt, List<ChatMessageDto>? history)
    {
        if (string.IsNullOrWhiteSpace(_geminiApiKey))
        {
            _logger.LogWarning("[AiChatService] Gemini:ApiKey is not configured.");
            return string.Empty;
        }

        // Active Gemini models in order of speed and capability
        var modelsToTry = new[]
        {
            "gemini-3.5-flash-lite",
            "gemini-3.1-flash-lite",
            "gemini-3.5-flash",
            "gemini-3.8-flash"
        };

        // Build multi-turn conversational contents
        var contentsList = new List<object>();

        if (history != null && history.Count > 0)
        {
            // Include recent dialogue turns for contextual continuity (last 10 turns)
            var recentTurns = history.TakeLast(10);
            foreach (var turn in recentTurns)
            {
                var role = turn.Role?.ToLower() == "user" ? "user" : "model";
                if (!string.IsNullOrWhiteSpace(turn.Content))
                {
                    contentsList.Add(new
                    {
                        role = role,
                        parts = new[] { new { text = turn.Content } }
                    });
                }
            }
        }

        // Current user message
        contentsList.Add(new
        {
            role = "user",
            parts = new[] { new { text = userPrompt } }
        });

        var requestBody = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            contents = contentsList
        };

        var jsonBody = JsonSerializer.Serialize(requestBody);

        foreach (var model in modelsToTry)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(20);
                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_geminiApiKey}";

                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("[AiChatService] Model {Model} returned HTTP {StatusCode}: {ErrorBody}", model, response.StatusCode, errBody);
                    continue;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);

                if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var candidateContent) &&
                        candidateContent.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0 &&
                        parts[0].TryGetProperty("text", out var textEl))
                    {
                        var text = textEl.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            _logger.LogInformation("[AiChatService] Successfully generated AI response via model: {Model}", model);
                            return text;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AiChatService] Exception while calling model {Model}", model);
            }
        }

        return string.Empty;
    }
}