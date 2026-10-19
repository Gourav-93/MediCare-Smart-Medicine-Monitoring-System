namespace MediCare.DTOs;

public class AdherencePointDto
{
    public string PeriodLabel { get; set; } = string.Empty;
    public int TotalDoses { get; set; }
    public int TakenDoses { get; set; }
    public int MissedDoses { get; set; }
    public double AdherencePercentage { get; set; }
}

public class AdherenceReportDto
{
    public int PatientId { get; set; }
    public string PeriodType { get; set; } = "Daily";
    public double OverallAdherence { get; set; }
    public List<AdherencePointDto> Timeline { get; set; } = new List<AdherencePointDto>();
}