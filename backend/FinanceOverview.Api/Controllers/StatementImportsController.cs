using System.Globalization;
using FinanceOverview.Api.Data;
using FinanceOverview.Api.Dtos;
using FinanceOverview.Api.Models;
using FinanceOverview.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceOverview.Api.Controllers;

[ApiController]
[Route("api/imports")]
public class StatementImportsController : ControllerBase
{
    private static readonly string[] AllowedContentTypes = ["application/pdf"];
    private readonly AppDbContext _dbContext;
    private readonly ImportStorageService _storageService;
    private readonly ExtractedTextStorageService _extractedTextStorage;
    private readonly IPdfTextExtractor _textExtractor;
    private readonly IStatementParserSelector _parserSelector;

    public StatementImportsController(
        AppDbContext dbContext,
        ImportStorageService storageService,
        ExtractedTextStorageService extractedTextStorage,
        IPdfTextExtractor textExtractor,
        IStatementParserSelector parserSelector)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _extractedTextStorage = extractedTextStorage;
        _textExtractor = textExtractor;
        _parserSelector = parserSelector;
    }

    [HttpPost]
    public async Task<ActionResult<ImportBatchDto>> Upload(
        [FromForm] CreateImportBatchRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Pdf is null || request.Pdf.Length == 0)
        {
            return BadRequest(new { error = "A non-empty PDF file is required." });
        }

        if (!IsPdf(request.Pdf))
        {
            return BadRequest(new { error = "Only PDF files are supported." });
        }

        if (string.IsNullOrWhiteSpace(request.StatementMonth)
            || !TryParseStatementMonth(request.StatementMonth, out var statementMonth))
        {
            return BadRequest(new { error = "Statement month must be in YYYY-MM format." });
        }

        var storedFile = await _storageService.SavePdfAsync(request.Pdf, cancellationToken);

        var existingBatch = await _dbContext.ImportBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(batch =>
                batch.StatementMonth == statementMonth
                && batch.Sha256Hash == storedFile.Sha256Hash,
                cancellationToken);

        if (existingBatch is not null)
        {
            if (System.IO.File.Exists(storedFile.FullPath))
            {
                System.IO.File.Delete(storedFile.FullPath);
            }

            return Ok(ToDto(existingBatch));
        }

        var importBatch = new ImportBatch
        {
            UploadedAt = DateTime.UtcNow,
            OriginalFileName = Path.GetFileName(request.Pdf.FileName),
            StatementMonth = statementMonth,
            Status = ImportBatchStatus.Uploaded,
            StorageKey = storedFile.StorageKey,
            Sha256Hash = storedFile.Sha256Hash
        };

        _dbContext.ImportBatches.Add(importBatch);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = importBatch.Id },
            ToDto(importBatch));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ImportBatchDto>>> Get()
    {
        var imports = await _dbContext.ImportBatches
            .AsNoTracking()
            .OrderByDescending(batch => batch.UploadedAt)
            .Select(batch => ToDto(batch))
            .ToListAsync();

        return Ok(imports);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ImportBatchDto>> GetById(int id)
    {
        var importBatch = await _dbContext.ImportBatches
            .AsNoTracking()
            .Where(batch => batch.Id == id)
            .Select(batch => ToDto(batch))
            .SingleOrDefaultAsync();

        if (importBatch is null)
        {
            return NotFound();
        }

        return Ok(importBatch);
    }

    [HttpPost("{id:int}/extract-text")]
    public async Task<ActionResult<ImportBatchDto>> ExtractText(int id, CancellationToken cancellationToken)
    {
        var importBatch = await _dbContext.ImportBatches
            .SingleOrDefaultAsync(batch => batch.Id == id, cancellationToken);

        if (importBatch is null)
        {
            return NotFound();
        }

        var pdfPath = _storageService.ResolveStoragePath(importBatch.StorageKey);

        if (!System.IO.File.Exists(pdfPath))
        {
            return NotFound(new { error = "Stored PDF not found." });
        }

        var extractedText = await _textExtractor.ExtractTextAsync(pdfPath, cancellationToken);
        await _extractedTextStorage.SaveExtractedTextAsync(importBatch.Id, extractedText, cancellationToken);

        importBatch.ExtractedAtUtc = DateTime.UtcNow;
        importBatch.ParserKey ??= _parserSelector.SelectParserKey(importBatch, extractedText);
        if (importBatch.Status is ImportBatchStatus.Uploaded or ImportBatchStatus.Failed)
        {
            importBatch.Status = ImportBatchStatus.Extracted;
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToDto(importBatch));
    }

    [HttpGet("{id:int}/extracted-text")]
    public async Task<IActionResult> GetExtractedText(int id, CancellationToken cancellationToken)
    {
        var importExists = await _dbContext.ImportBatches
            .AsNoTracking()
            .AnyAsync(batch => batch.Id == id, cancellationToken);

        if (!importExists)
        {
            return NotFound();
        }

        var extractedPath = _extractedTextStorage.GetExtractedTextPath(id);

        if (!System.IO.File.Exists(extractedPath))
        {
            return NotFound();
        }

        return PhysicalFile(extractedPath, "text/plain");
    }

    private static ImportBatchDto ToDto(ImportBatch batch)
    {
        return new ImportBatchDto(
            batch.Id,
            batch.UploadedAt,
            batch.ExtractedAtUtc,
            batch.OriginalFileName,
            batch.StatementMonth,
            batch.Status.ToString(),
            batch.StorageKey,
            batch.Sha256Hash,
            batch.ParserKey);
    }

    private static bool IsPdf(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);
        return AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase)
            || string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseStatementMonth(string value, out DateOnly statementMonth)
    {
        if (DateTime.TryParseExact(
                value,
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedMonth))
        {
            statementMonth = new DateOnly(parsedMonth.Year, parsedMonth.Month, 1);
            return true;
        }

        if (DateOnly.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
        {
            statementMonth = new DateOnly(parsedDate.Year, parsedDate.Month, 1);
            return true;
        }

        statementMonth = default;
        return false;
    }
}
