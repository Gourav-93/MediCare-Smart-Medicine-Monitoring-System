using MediCare.Data;
using MediCare.Models;
using MediCare.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Repositories;

public class MedicineRepository : IMedicineRepository
{
    private readonly AppDbContext _context;

    public MedicineRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Medicine>> GetAllAsync()
    {
        return await _context.Medicines
            .ToListAsync();
    }

    public async Task<Medicine?> GetByIdAsync(int id)
    {
        return await _context.Medicines
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Medicine> AddAsync(Medicine medicine)
    {
        _context.Medicines.Add(medicine);
        await _context.SaveChangesAsync();

        return medicine;
    }

    public async Task<Medicine> UpdateAsync(Medicine medicine)
    {
        _context.Medicines.Update(medicine);
        await _context.SaveChangesAsync();

        return medicine;
    }

    public async Task DeleteAsync(Medicine medicine)
    {
        _context.Medicines.Remove(medicine);
        await _context.SaveChangesAsync();
    }
}