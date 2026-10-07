using MediCare.Data;
using MediCare.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Service;

public class AdherenceService : IAdherenceService
{
    private readonly AppDbContext _context;

    public AdherenceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<object> GetPatientAdherenceAsync(int patientId)
    {
        var logs = await _context.MedicineLogs
            .Where(x => x.PatientId == patientId)
            .ToListAsync();

        var totalDoses = logs.Count;

        var takenDoses = logs.Count(x =>
            x.Status == "Taken");

        var missedDoses = logs.Count(x =>
            x.Status == "Missed");

        double adherencePercentage = totalDoses == 0
            ? 0
            : (double)takenDoses / totalDoses * 100;

        return new
        {
            PatientId = patientId,
            TotalDoses = totalDoses,
            TakenDoses = takenDoses,
            MissedDoses = missedDoses,
            AdherencePercentage = Math.Round(adherencePercentage, 2)
        };
    }
}