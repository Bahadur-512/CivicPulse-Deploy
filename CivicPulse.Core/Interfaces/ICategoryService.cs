using CivicPulse.Core.DTOs;

namespace CivicPulse.Core.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<CategoryDto> GetByIdAsync(int id);
    Task<CategoryDto> CreateAsync(CategoryDto dto);
    Task<CategoryDto> UpdateAsync(CategoryDto dto);
    Task<bool> DeleteAsync(int id);
    Task<List<CategoryDto>> GetActiveAsync();
}
