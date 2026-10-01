using MediCare.Data;

namespace MediCare.Models;

public class Patient
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime DateOfBirth { get; set; }

    public string EmergencyContact { get; set; } = string.Empty;

    public User User { get; set; } = null!;      

    public ICollection<CaregiverPatient> CaregiverPatients { get; set; }
                = new List<CaregiverPatient>();
}