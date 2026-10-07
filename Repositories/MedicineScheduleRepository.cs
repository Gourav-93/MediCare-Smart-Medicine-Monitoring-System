using MediCare.Data;
using MediCare.Models;
using MediCare.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Repositories;

public class MedicineScheduleRepository : IMedicineScheduleRepository
{
    private readonly AppDbContext _context;

    public MedicineScheduleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<MedicineSchedule>> GetAllAsync()
    {
        return await _context.MedicineSchedules.ToListAsync();
    }

    public async Task<MedicineSchedule?> GetByIdAsync(int id)
    {
        return await _context.MedicineSchedules
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<MedicineSchedule> AddAsync(MedicineSchedule schedule)
    {
        _context.MedicineSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        return schedule;
    }

    public async Task<MedicineSchedule> UpdateAsync(MedicineSchedule schedule)
    {
        _context.MedicineSchedules.Update(schedule);
        await _context.SaveChangesAsync();

        return schedule;
    }

    public async Task DeleteAsync(MedicineSchedule schedule)
    {
        _context.MedicineSchedules.Remove(schedule);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> MedicineExistsAsync(int medicineId)
    {
        return await _context.Medicines.AnyAsync(m => m.Id == medicineId);
    }
}