namespace mediCare.DTOs;

public class ChatRequestDto
{
    public string Message { get; set; } = string.Empty;
    public int? TargetPatientId { get; set; }
}

public class ChatResponseDto
{
    public string Reply { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public List<string> QuickSuggestions { get; set; } = new();
}