using MediCare.Data;

namespace MediCare.Models;

public class Caregiver
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Relationship { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}