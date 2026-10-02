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
    }
}