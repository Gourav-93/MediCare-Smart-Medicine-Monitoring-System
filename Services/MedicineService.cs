using mediCare.DTOs;
using MediCare.Data;
using MediCare.Models;
using MediCare.Repositories.Interfaces;
using MediCare.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Services;

public class MedicineService : IMedicineService
{
    private readonly IMedicineRepository _repository;
    private readonly AppDbContext _context;

    public MedicineService(IMedicineRepository repository, AppDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<IEnumerable<Medicine>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Medicine?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Medicine> AddAsync(MedicineDto dto)
    {
        var medicine = new Medicine
        {
            Name = dto.Name,
            Dosage = dto.Dosage,
            Dose = dto.Dose,
            Stock = dto.Stock,
            PatientId = dto.PatientId,
            MealTiming = string.IsNullOrWhiteSpace(dto.MealTiming) ? "AfterMeal" : dto.MealTiming
        };

        return await _repository.AddAsync(medicine);
    }

    public async Task<Medicine?> UpdateAsync(int id, MedicineDto dto)
    {
        var existingMedicine = await _repository.GetByIdAsync(id);
        if (existingMedicine == null)
            return null;

        existingMedicine.Name = dto.Name;
        existingMedicine.Dosage = dto.Dosage;
        existingMedicine.Dose = dto.Dose;
        existingMedicine.Stock = dto.Stock;
        existingMedicine.MealTiming = string.IsNullOrWhiteSpace(dto.MealTiming) 
            ? existingMedicine.MealTiming 
            : dto.MealTiming;

        return await _repository.UpdateAsync(existingMedicine);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var medicine = await _repository.GetByIdAsync(id);
        if (medicine == null)
            return false;

        return await _repository.DeleteAsync(id);
    }
}