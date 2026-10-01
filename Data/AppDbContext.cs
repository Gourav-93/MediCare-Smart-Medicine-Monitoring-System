using MediCare.Models;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> User { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Caregiver> Caregivers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Patient>()
        .HasOne(p => p.User)
        .WithOne(u => u.Patient)
        .HasForeignKey<Patient>(p => p.UserId)
        .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Caregiver>()
        .HasOne(c => c.User)
        .WithOne(u => u.Caregiver)
        .HasForeignKey<Caregiver>(c => c.UserId)
        .OnDelete(DeleteBehavior.Cascade);
    }

}