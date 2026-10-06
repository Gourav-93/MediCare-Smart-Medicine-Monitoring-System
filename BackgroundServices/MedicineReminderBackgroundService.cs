using MediCare.Data;
using Microsoft.EntityFrameworkCore;

namespace MediCare.BackgroundServices;

public class MedicineReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MedicineReminderBackgroundService> _logger;

    public MedicineReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<MedicineReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("Medicine Reminder Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckMedicineSchedulesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while checking medicine schedules.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromMinutes(1),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task CheckMedicineSchedulesAsync(
        CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var now = DateTime.Now;
        var today = now.Date;

        var medicines = await context.Medicines
            .Include(m => m.Schedules)
            .ToListAsync(stoppingToken);

        foreach (var medicine in medicines)
        {
            // Check medicine's active date range.
            if (medicine.StartDate.Date > today ||
                medicine.EndDate.Date < today)
            {
                continue;
            }

            foreach (var schedule in medicine.Schedules)
            {
                // Currently supporting Daily schedules.
                if (!string.Equals(
                    schedule.Frequency,
                    "Daily",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var scheduledTime = today.Add(schedule.Time);

                // Do not process future doses.
                if (scheduledTime > now)
                {
                    continue;
                }

                var log = await context.MedicineLogs
                    .FirstOrDefaultAsync(
                        x => x.MedicineId == medicine.Id &&
                             x.ScheduledTime == scheduledTime,
                        stoppingToken);

                // Create a log only if one does not exist.
                if (log == null)
                {
                    log = new MediCare.Models.MedicineLog
                    {
                        MedicineId = medicine.Id,
                        PatientId = medicine.PatientId,
                        ScheduledTime = scheduledTime,
                        Status = "Pending"
                    };

                    context.MedicineLogs.Add(log);
                }

                // Mark the dose missed after 30 minutes.
                if (log.Status == "Pending" &&
                    now >= scheduledTime.AddMinutes(30))
                {
                    log.Status = "Missed";
                }
            }
        }

        await context.SaveChangesAsync(stoppingToken);
    }
}