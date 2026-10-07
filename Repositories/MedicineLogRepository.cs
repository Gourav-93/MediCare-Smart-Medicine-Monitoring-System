using MediCare.Data;
using MediCare.Models;
using MediCare.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Repositories;

public class MedicineLogRepository : IMedicineLogRepository
{
    private readonly AppDbContext _context;

    public MedicineLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<MedicineLog>> GetAllAsync()
    {
        return await _context.MedicineLogs
            .ToListAsync();
    }

    public async Task<MedicineLog?> GetByIdAsync(int id)
    {
        return await _context.MedicineLogs
            .Include(x => x.Medicine)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<MedicineLog> AddAsync(MedicineLog log)
    {
        _context.MedicineLogs.Add(log);
        await _context.SaveChangesAsync();

        return log;
    }

    public async Task<MedicineLog> UpdateAsync(MedicineLog log)
    {
        _context.MedicineLogs.Update(log);
        await _context.SaveChangesAsync();

        return log;
    }

    public async Task DeleteAsync(int id)
    {
        var log = await _context.MedicineLogs
            .FirstOrDefaultAsync(x => x.Id == id);

        if (log != null)
        {
            _context.MedicineLogs.Remove(log);
            await _context.SaveChangesAsync();
        }
    }
}