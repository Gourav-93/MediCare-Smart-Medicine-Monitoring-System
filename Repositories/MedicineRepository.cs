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
        return await _context.Medicines.ToListAsync();
    }

}