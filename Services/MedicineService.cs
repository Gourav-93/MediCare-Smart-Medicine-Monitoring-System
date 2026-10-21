using MediCare.Data;
using MediCare.DTOs;
using MediCare.Models;
using MediCare.Repositories.Interfaces;
using MediCare.Services.Interfaces;

namespace MediCare.Services;

public class MedicineService : IMedicineService
{
    private readonly IMedicineRepository _repository;

    public MedicineService(IMedicineRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Medicine>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Medicine?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Medicine> AddAsync(MedicineDto dto)
    {
        if (!await _repository.PatientExistsAsync(dto.PatientId))
        {
            throw new ArgumentException("Patient not found.");
        }

        var medicine = new Medicine
        {
            PatientId = dto.PatientId,
            Name = dto.Name,
            Dosage = dto.Dosage,
            Dose = dto.Dose,
            Stock = dto.Stock,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            MealTimeing = string.IsNullOrWhiteSpace(dto.MealTiming) ? "AfterMeal" : dto.MealTiming
        };

        return await _repository.AddAsync(medicine);
    }

    public async Task<Medicine?> UpdateAsync(int id, MedicineDto dto)
    {
        if (!await _repository.PatientExistsAsync(dto.PatientId))
        {
            throw new ArgumentException("Patient not found.");
        }

        var existingMedicine = await _repository.GetByIdAsync(id);
        if (existingMedicine == null)
            return null;

        existingMedicine.PatientId = dto.PatientId;
        existingMedicine.Name = dto.Name;
        existingMedicine.Dosage = dto.Dosage;
        existingMedicine.Dose = dto.Dose;
        existingMedicine.Stock = dto.Stock;
        existingMedicine.StartDate = dto.StartDate;
        existingMedicine.EndDate = dto.EndDate;
        existingMedicine.MealTimeing = string.IsNullOrWhiteSpace(dto.MealTiming) 
            ? existingMedicine.MealTimeing 
            : dto.MealTiming;

        return await _repository.UpdateAsync(existingMedicine);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var medicine = await _repository.GetByIdAsync(id);
        if (medicine == null)
            return false;

        await _repository.DeleteAsync(medicine);
        return true;
    }
}