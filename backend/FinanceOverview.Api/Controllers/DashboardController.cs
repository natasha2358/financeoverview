using System.Globalization;
using FinanceOverview.Api.Dtos;
using FinanceOverview.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceOverview.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly DashboardSummaryService _dashboardSummaryService;

    public DashboardController(DashboardSummaryService dashboardSummaryService)
    {
        _dashboardSummaryService = dashboardSummaryService;
    }

    [HttpPost("monthly-summary")]
    public async Task<ActionResult<MonthlySummaryResponseDto>> GetMonthlySummary(
        [FromBody] MonthlySummaryRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(
                request.Month,
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var monthStart))
        {
            return ValidationProblem("Month must be in YYYY-MM format.");
        }

        var summary = await _dashboardSummaryService.GetMonthlySummaryAsync(monthStart, cancellationToken);
        return Ok(summary);
    }
}
