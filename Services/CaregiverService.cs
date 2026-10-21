using Medicare.DTOs;
using MediCare.Data;
using MediCare.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Services;

public class CaregiverService : ICaregiverService
{
    private readonly AppDbContext _context;

    public CaregiverService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CaregiverPatientCardDto>> GetLinkedPatientsCardsAsync(int currentUserId)
    {
        var caregiver = await _context.Caregivers
            .FirstOrDefaultAsync(c => c.UserId == currentUserId);

        if (caregiver == null)
            return Enumerable.Empty<CaregiverPatientCardDto>();

        var today = DateTime.Today;

        var linkedPatients = await _context.CaregiverPatients
            .Where(cp => cp.CaregiverId == caregiver.Id)
            .Include(cp => cp.Patient)
                .ThenInclude(p => p.User)
            .ToListAsync();

        var result = new List<CaregiverPatientCardDto>();

        foreach (var item in linkedPatients)
        {
            var p = item.Patient;

            // Fetch logs for today (Pending, Snoozed, Missed, Taken)
            var logsToday = await _context.MedicineLogs
                .Where(l => l.PatientId == p.Id && l.ScheduledTime.Date == today)
                .ToListAsync();

            // Fetch historical logs to compute adherence score
            var allHistoricalLogs = await _context.MedicineLogs
                .Where(l => l.PatientId == p.Id && (l.Status == "Taken" || l.Status == "Missed"))
                .ToListAsync();

            var totalDoses = allHistoricalLogs.Count;
            var takenDoses = allHistoricalLogs.Count(l => l.Status == "Taken");
            var adherence = totalDoses == 0 ? 0 : Math.Round((double)takenDoses / totalDoses * 100, 1);

            result.Add(new CaregiverPatientCardDto
            {
                PatientId = p.Id,
                PatientName = p.User?.Name ?? "Patient",
                Email = p.User?.Email ?? string.Empty,
                PhoneNumber = p.EmergencyContact ?? string.Empty,
                EmergencyContact = p.EmergencyContact ?? string.Empty,
                AdherenceRate = adherence,
                PendingDosesToday = logsToday.Count(l => l.Status == "Pending" || l.Status == "Snoozed"),
                MissedDosesToday = logsToday.Count(l => l.Status == "Missed")
            });
        }

        return result;
    }
}