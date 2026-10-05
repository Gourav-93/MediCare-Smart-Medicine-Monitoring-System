using MediCare.DTOs;
using MediCare.Models;
using MediCare.Repositories.Interfaces;
using MediCare.Services.Interfaces;

namespace MediCare.Service;

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
        var medicine = new Medicine
        {
            PatientId = dto.PatientId,
            Name = dto.Name,
            Dosage = dto.Dosage,
            Dose = dto.Dose,
            Stock = dto.Stock,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };
        return await _repository.AddAsync(medicine);
    }

    public async Task<Medicine> UpdateAsync(int id, MedicineDto dto)
    {
        var medicine = await _repository.GetByIdAsync(id);
        if (medicine == null)
        {
            return null;
        }
        medicine.PatientId = dto.PatientId;
        medicine.Name = dto.Name;
        medicine.Dosage = dto.Dosage;
        medicine.Dose = dto.Dose;
        medicine.Stock = dto.Stock;
        medicine.StartDate = dto.StartDate;
        medicine.EndDate = dto.EndDate;

        return await _repository.UpdateAsync(medicine);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var medicine = await _repository.GetByIdAsync(id);
        if (medicine == null)
        {
            return false;
        }
        await _repository.DeleteAsync(medicine);
        return true;
    }
}