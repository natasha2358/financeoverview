using Microsoft.AspNetCore.Http;

namespace FinanceOverview.Api.Dtos;

public sealed class CreateImportBatchRequest
{
    public IFormFile? Pdf { get; init; }
    public string? StatementMonth { get; init; }
}
