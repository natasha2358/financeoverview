using FinanceOverview.Api.Data;
using FinanceOverview.Api.Dtos;
using FinanceOverview.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceOverview.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public CategoriesController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> Get()
    {
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryDto(category.Id, category.Name))
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryRequest request)
    {
        var normalizedName = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return BadRequest(new { error = "Category name is required." });
        }

        var normalizedLower = normalizedName.ToLowerInvariant();
        var exists = await _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(category => category.Name.ToLower() == normalizedLower);

        if (exists)
        {
            return Conflict(new { error = "Category already exists." });
        }

        var category = new Category { Name = normalizedName };
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        var response = new CategoryDto(category.Id, category.Name);
        return Created("/api/categories", response);
    }
}
