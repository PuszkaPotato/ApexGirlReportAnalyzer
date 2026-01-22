using ApexGirlReportAnalyzer.Infrastructure.Data;
using ApexGirlReportAnalyzer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApexGirlReportAnalyzer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TiersController(AppDbContext context) : ControllerBase
{
    private readonly AppDbContext _context = context;

    /// <summary>
    /// Get all tiers with their limits
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TierResponse>), 200)]
    public async Task<IActionResult> GetTiers()
    {
        var tiers = await _context.Tiers
            .Include(t => t.TierLimits)
            .Select(t => new TierResponse
            {
                Id = t.Id,
                Name = t.Name,
                Limits = t.TierLimits.Select(l => new TierLimitResponse
                {
                    Scope = l.Scope.ToString(),
                    DailyRequestLimit = l.DailyRequestLimit,
                    MonthlyRequestLimit = l.MonthlyRequestLimit
                }).ToList()
            })
            .ToListAsync();

        return Ok(tiers);
    }
}