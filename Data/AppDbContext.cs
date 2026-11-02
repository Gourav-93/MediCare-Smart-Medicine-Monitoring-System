using MediCare.Models;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    public DbSet<Patient> Patients { get; set; }

    public DbSet<Caregiver> Caregivers { get; set; }

    public DbSet<CaregiverPatient> CaregiverPatients { get; set; }

    public DbSet<Medicine> Medicines { get; set; }

    public DbSet<MedicineSchedule> MedicineSchedules { get; set; }

    public DbSet<MedicineLog> MedicineLogs { get; set; }

    public DbSet<Notification> Notifications { get; set; }

    public DbSet<EmergencyAlert> EmergencyAlerts { get; set; }

    public DbSet<EmailDeliveryLog> EmailDeliveryLogs { get; set; }

    public DbSet<UserNotificationPreference> UserNotificationPreferences { get; set; }

    public DbSet<EmergencyContact> EmergencyContacts { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User -> Patient (One-to-One)
        modelBuilder.Entity<Patient>()
            .HasOne(p => p.User)
            .WithOne(u => u.Patient)
            .HasForeignKey<Patient>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // User -> Caregiver (One-to-One)
        modelBuilder.Entity<Caregiver>()
            .HasOne(c => c.User)
            .WithOne(u => u.Caregiver)
            .HasForeignKey<Caregiver>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Caregiver -> CaregiverPatient (One-to-Many)
        modelBuilder.Entity<CaregiverPatient>()
            .HasOne(cp => cp.Caregiver)
            .WithMany(c => c.CaregiverPatients)
            .HasForeignKey(cp => cp.CaregiverId)
            .OnDelete(DeleteBehavior.Restrict);

        // Patient -> CaregiverPatient (One-to-Many)
        modelBuilder.Entity<CaregiverPatient>()
            .HasOne(cp => cp.Patient)
            .WithMany(p => p.CaregiverPatients)
            .HasForeignKey(cp => cp.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Same caregiver cannot link to same patient twice
        modelBuilder.Entity<CaregiverPatient>()
            .HasIndex(cp => new { cp.CaregiverId, cp.PatientId })
            .IsUnique();


        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Medicine -> MedicineSchedule (One-to-Many)
        modelBuilder.Entity<MedicineSchedule>()
            .HasOne(ms => ms.Medicine)
            .WithMany(m => m.MedicineSchedules)
            .HasForeignKey(ms => ms.MedicineId)
            .OnDelete(DeleteBehavior.Cascade);

        // Medicine -> MedicineLog (One-to-Many, Prevent Cascade Delete of History)
        modelBuilder.Entity<MedicineLog>()
            .HasOne(ml => ml.Medicine)
            .WithMany(m => m.MedicineLogs)
            .HasForeignKey(ml => ml.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        // Schedule -> MedicineLog (Optional One-to-Many)
        modelBuilder.Entity<MedicineLog>()
            .HasOne(ml => ml.Schedule)
            .WithMany()
            .HasForeignKey(ml => ml.ScheduleId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<EmergencyAlert>()
            .HasOne(e => e.Patient)
            .WithMany()
            .HasForeignKey(e => e.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EmailDeliveryLog>()
            .HasIndex(e => new
            {
                e.MedicineScheduleId,
                e.ScheduledOccurrence,
                e.NotificationType,
                e.RecipientEmail
            })
            .IsUnique();

        modelBuilder.Entity<EmailDeliveryLog>()
            .HasOne(e => e.MedicineSchedule)
            .WithMany()
            .HasForeignKey(e => e.MedicineScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserNotificationPreference>()
            .HasOne(p => p.User)
            .WithOne(u => u.NotificationPreference)
            .HasForeignKey<UserNotificationPreference>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EmergencyContact>()
            .HasOne(ec => ec.Patient)
            .WithMany(p => p.EmergencyContacts)
            .HasForeignKey(ec => ec.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
