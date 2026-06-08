using Microsoft.EntityFrameworkCore;
using CivicPulse.Core.DTOs;
using CivicPulse.Core.Entities;
using CivicPulse.Core.Enums;
using CivicPulse.Core.Interfaces;

namespace CivicPulse.Web.Services;

public class CategoryService : ICategoryService
{
    private readonly IRepository<Category> _categoryRepo;

    public CategoryService(IRepository<Category> categoryRepo)
    {
        _categoryRepo = categoryRepo;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var categories = await _categoryRepo.Query()
            .OrderBy(c => c.Name)
            .ToListAsync();

        return categories.Select(MapToDto).ToList();
    }

    public async Task<CategoryDto> GetByIdAsync(int id)
    {
        var category = await _categoryRepo.Query()
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Category not found.");

        return MapToDto(category);
    }

    public async Task<CategoryDto> CreateAsync(CategoryDto dto)
    {
        var category = new Category
        {
            Name = dto.Name,
            Description = dto.Description,
            IconName = dto.IconName,
            ColorHex = dto.ColorHex,
            DefaultSlaDays = dto.DefaultSlaDays,
            DefaultPriority = Enum.Parse<PriorityLevel>(dto.DefaultPriority),
            IsActive = dto.IsActive,
            Keywords = dto.Keywords
        };

        await _categoryRepo.AddAsync(category);
        await _categoryRepo.SaveChangesAsync();

        dto.Id = category.Id;
        return dto;
    }

    public async Task<CategoryDto> UpdateAsync(CategoryDto dto)
    {
        var category = await _categoryRepo.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException("Category not found.");

        category.Name = dto.Name;
        category.Description = dto.Description;
        category.IconName = dto.IconName;
        category.ColorHex = dto.ColorHex;
        category.DefaultSlaDays = dto.DefaultSlaDays;
        category.DefaultPriority = Enum.Parse<PriorityLevel>(dto.DefaultPriority);
        category.IsActive = dto.IsActive;
        category.Keywords = dto.Keywords;

        _categoryRepo.Update(category);
        await _categoryRepo.SaveChangesAsync();

        return dto;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _categoryRepo.GetByIdAsync(id);
        if (category == null) return false;

        category.IsActive = false;
        _categoryRepo.Update(category);
        await _categoryRepo.SaveChangesAsync();
        return true;
    }

    public async Task<List<CategoryDto>> GetActiveAsync()
    {
        var categories = await _categoryRepo.Query()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return categories.Select(MapToDto).ToList();
    }

    private static CategoryDto MapToDto(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        IconName = c.IconName,
        ColorHex = c.ColorHex,
        DefaultSlaDays = c.DefaultSlaDays,
        DefaultPriority = c.DefaultPriority.ToString(),
        IsActive = c.IsActive,
        Keywords = c.Keywords
    };
}
