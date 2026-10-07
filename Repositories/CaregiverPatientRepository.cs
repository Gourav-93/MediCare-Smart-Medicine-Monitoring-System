using MediCare.Data;
using MediCare.Models;
using MediCare.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Repositories;

public class CaregiverPatientRepository : ICaregiverPatientRepository
{
    private readonly AppDbContext _context;

    public CaregiverPatientRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CaregiverPatient> AddAsync(
        CaregiverPatient caregiverPatient)
    {
        _context.CaregiverPatients.Add(caregiverPatient);
        await _context.SaveChangesAsync();

        return caregiverPatient;
    }

    public async Task<List<CaregiverPatient>> GetByCaregiverIdAsync(
        int caregiverId)
    {
        return await _context.CaregiverPatients
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .Where(x => x.CaregiverId == caregiverId)
            .ToListAsync();
    }

    public async Task<List<CaregiverPatient>> GetByPatientIdAsync(
        int patientId)
    {
        return await _context.CaregiverPatients
            .Include(x => x.Caregiver)
            .ThenInclude(x => x.User)
            .Where(x => x.PatientId == patientId)
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(
        int caregiverId,
        int patientId)
    {
        var relationship = await _context.CaregiverPatients
            .FirstOrDefaultAsync(x =>
                x.CaregiverId == caregiverId &&
                x.PatientId == patientId);

        if (relationship == null)
        {
            return false;
        }

        _context.CaregiverPatients.Remove(relationship);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CaregiverExistsAsync(int caregiverId)
    {
        return await _context.Caregivers.AnyAsync(c => c.Id == caregiverId);
    }

    public async Task<bool> PatientExistsAsync(int patientId)
    {
        return await _context.Patients.AnyAsync(p => p.Id == patientId);
    }
}