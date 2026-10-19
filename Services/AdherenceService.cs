using MediCare.DTOs;
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
        var takenDoses = logs.Count(x => x.Status == "Taken");
        var missedDoses = logs.Count(x => x.Status == "Missed");

        double adherencePercentage = totalDoses == 0 ? 0 : (double)takenDoses / totalDoses * 100;

        return new
        {
            PatientId = patientId,
            TotalDoses = totalDoses,
            TakenDoses = takenDoses,
            MissedDoses = missedDoses,
            AdherencePercentage = Math.Round(adherencePercentage, 2)
        };
    }

    public async Task<AdherenceReportDto> GetPeriodicAdherenceAsync(int patientId, string period)
    {
        var report = new AdherenceReportDto
        {
            PatientId = patientId,
            PeriodType = period
        };

        var now = DateTime.Today;
        var query = _context.MedicineLogs
            .Where(x => x.PatientId == patientId && (x.Status == "Taken" || x.Status == "Missed"));

        if (period.Equals("Daily", StringComparison.OrdinalIgnoreCase))
        {
            // Last 7 Days
            var startDate = now.AddDays(-6);
            var logs = await query.Where(x => x.ScheduledTime.Date >= startDate).ToListAsync();

            for (int i = 0; i < 7; i++)
            {
                var day = startDate.AddDays(i);
                var dayLogs = logs.Where(x => x.ScheduledTime.Date == day).ToList();
                var taken = dayLogs.Count(x => x.Status == "Taken");
                var total = dayLogs.Count;

                report.Timeline.Add(new AdherencePointDto
                {
                    PeriodLabel = day.ToString("ddd, dd MMM"),
                    TotalDoses = total,
                    TakenDoses = taken,
                    MissedDoses = dayLogs.Count(x => x.Status == "Missed"),
                    AdherencePercentage = total == 0 ? 0 : Math.Round((double)taken / total * 100, 2)
                });
            }
        }
        else if (period.Equals("Weekly", StringComparison.OrdinalIgnoreCase))
        {
            // Last 4 Weeks
            var startDate = now.AddDays(-27);
            var logs = await query.Where(x => x.ScheduledTime.Date >= startDate).ToListAsync();

            for (int i = 0; i < 4; i++)
            {
                var weekStart = startDate.AddDays(i * 7);
                var weekEnd = weekStart.AddDays(6);
                var weekLogs = logs.Where(x => x.ScheduledTime.Date >= weekStart && x.ScheduledTime.Date <= weekEnd).ToList();
                var taken = weekLogs.Count(x => x.Status == "Taken");
                var total = weekLogs.Count;

                report.Timeline.Add(new AdherencePointDto
                {
                    PeriodLabel = $"{weekStart:dd MMM} - {weekEnd:dd MMM}",
                    TotalDoses = total,
                    TakenDoses = taken,
                    MissedDoses = weekLogs.Count(x => x.Status == "Missed"),
                    AdherencePercentage = total == 0 ? 0 : Math.Round((double)taken / total * 100, 2)
                });
            }
        }
        else // Monthly (Last 6 Months)
        {
            var startDate = new DateTime(now.Year, now.Month, 1).AddMonths(-5);
            var logs = await query.Where(x => x.ScheduledTime >= startDate).ToListAsync();

            for (int i = 0; i < 6; i++)
            {
                var monthDate = startDate.AddMonths(i);
                var monthLogs = logs.Where(x => x.ScheduledTime.Year == monthDate.Year && x.ScheduledTime.Month == monthDate.Month).ToList();
                var taken = monthLogs.Count(x => x.Status == "Taken");
                var total = monthLogs.Count;

                report.Timeline.Add(new AdherencePointDto
                {
                    PeriodLabel = monthDate.ToString("MMM yyyy"),
                    TotalDoses = total,
                    TakenDoses = taken,
                    MissedDoses = monthLogs.Count(x => x.Status == "Missed"),
                    AdherencePercentage = total == 0 ? 0 : Math.Round((double)taken / total * 100, 2)
                });
            }
        }

        var totalDosesAll = report.Timeline.Sum(x => x.TotalDoses);
        var takenDosesAll = report.Timeline.Sum(x => x.TakenDoses);
        report.OverallAdherence = totalDosesAll == 0 ? 0 : Math.Round((double)takenDosesAll / totalDosesAll * 100, 2);

        return report;
    }
}