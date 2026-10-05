namespace MediCare.DTOs;

public class MedicineScheduleDto
{
    public int MedicineId{ get; set; }
    public TimeSpan Time{ get; set; }
    public string Frequency{ get; set; } = string.Empty; 
}