using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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
    private readonly IStatementParserRegistry _parserRegistry;
    private readonly MerchantRuleService _merchantRuleService;

    public StatementImportsController(
        AppDbContext dbContext,
        ImportStorageService storageService,
        ExtractedTextStorageService extractedTextStorage,
        IPdfTextExtractor textExtractor,
        IStatementParserSelector parserSelector,
        IStatementParserRegistry parserRegistry,
        MerchantRuleService merchantRuleService)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _extractedTextStorage = extractedTextStorage;
        _textExtractor = textExtractor;
        _parserSelector = parserSelector;
        _parserRegistry = parserRegistry;
        _merchantRuleService = merchantRuleService;
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

    [HttpPost("{id:int}/parse-to-staging")]
    public async Task<ActionResult<IReadOnlyList<StagedTransactionDto>>> ParseToStaging(
        int id,
        CancellationToken cancellationToken)
    {
        var importBatch = await _dbContext.ImportBatches
            .SingleOrDefaultAsync(batch => batch.Id == id, cancellationToken);

        if (importBatch is null)
        {
            return NotFound();
        }

        var extractedPath = _extractedTextStorage.GetExtractedTextPath(id);
        if (!System.IO.File.Exists(extractedPath))
        {
            return BadRequest(new { error = "Extracted text is required before parsing." });
        }

        var extractedText = await System.IO.File.ReadAllTextAsync(extractedPath, cancellationToken);
        var parserKey = _parserSelector.SelectParserKey(importBatch, extractedText);

        if (string.Equals(parserKey, DefaultStatementParserSelector.UnknownParserKey, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Unsupported statement format." });
        }

        var parser = _parserRegistry.GetByKey(parserKey);
        if (parser is null)
        {
            return BadRequest(new { error = $"Parser '{parserKey}' is not available." });
        }

        var existingRows = await _dbContext.StagedTransactions
            .Where(row => row.ImportBatchId == id)
            .ToListAsync(cancellationToken);
        if (existingRows.Count > 0)
        {
            _dbContext.StagedTransactions.RemoveRange(existingRows);
        }

        var stagedRows = parser.Parse(importBatch.Id, extractedText);
        _dbContext.StagedTransactions.AddRange(stagedRows);

        importBatch.ParserKey = parserKey;
        importBatch.Status = ImportBatchStatus.Parsed;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = stagedRows.Select(ToDto).ToList();
        return Ok(response);
    }

    [HttpGet("{id:int}/staged-transactions")]
    public async Task<ActionResult<IReadOnlyList<StagedTransactionDto>>> GetStagedTransactions(
        int id,
        CancellationToken cancellationToken)
    {
        var importExists = await _dbContext.ImportBatches
            .AsNoTracking()
            .AnyAsync(batch => batch.Id == id, cancellationToken);

        if (!importExists)
        {
            return NotFound();
        }

        var rows = await _dbContext.StagedTransactions
            .AsNoTracking()
            .Where(row => row.ImportBatchId == id)
            .OrderBy(row => row.RowIndex)
            .Select(row => ToDto(row))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPut("{id:int}/staged-transactions/{stagedId:int}/approval")]
    public async Task<ActionResult<StagedTransactionDto>> UpdateStagedApproval(
        int id,
        int stagedId,
        [FromBody] UpdateStagedTransactionApprovalRequest request,
        CancellationToken cancellationToken)
    {
        var staged = await _dbContext.StagedTransactions
            .SingleOrDefaultAsync(row => row.ImportBatchId == id && row.Id == stagedId, cancellationToken);

        if (staged is null)
        {
            return NotFound();
        }

        staged.IsApproved = request.IsApproved;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToDto(staged));
    }

    [HttpPost("{id:int}/commit")]
    public async Task<ActionResult<CommitImportBatchResultDto>> Commit(
        int id,
        CancellationToken cancellationToken)
    {
        var importBatch = await _dbContext.ImportBatches
            .SingleOrDefaultAsync(batch => batch.Id == id, cancellationToken);

        if (importBatch is null)
        {
            return NotFound();
        }

        var approvedRows = await _dbContext.StagedTransactions
            .Where(row => row.ImportBatchId == id && row.IsApproved)
            .OrderBy(row => row.RowIndex)
            .ToListAsync(cancellationToken);

        var approvedCount = approvedRows.Count;
        var newTransactions = new List<Transaction>();

        if (approvedCount > 0)
        {
            var rowFingerprints = approvedRows
                .Select(BuildRowFingerprint)
                .ToList();

            var existingFingerprints = await _dbContext.Transactions
                .AsNoTracking()
                .Where(transaction =>
                    transaction.ImportBatchId == id
                    && transaction.RowFingerprint != null
                    && rowFingerprints.Contains(transaction.RowFingerprint))
                .Select(transaction => transaction.RowFingerprint!)
                .ToListAsync(cancellationToken);

            var existingSet = new HashSet<string>(existingFingerprints);

            foreach (var row in approvedRows)
            {
                var fingerprint = BuildRowFingerprint(row);
                if (existingSet.Contains(fingerprint))
                {
                    continue;
                }

                var currency = string.IsNullOrWhiteSpace(row.Currency) ? "EUR" : row.Currency.Trim();

                newTransactions.Add(new Transaction
                {
                    Date = row.BookingDate,
                    RawDescription = row.RawDescription.Trim(),
                    Merchant = row.RawDescription.Trim(),
                    Amount = row.Amount,
                    Currency = currency,
                    Balance = row.RunningBalance,
                    ImportBatchId = id,
                    RowFingerprint = fingerprint
                });
            }
        }

        if (newTransactions.Count > 0)
        {
            await _merchantRuleService.ApplyRulesAsync(newTransactions, cancellationToken);
            _dbContext.Transactions.AddRange(newTransactions);
        }

        importBatch.Status = ImportBatchStatus.Committed;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var committedCount = newTransactions.Count;
        var skippedCount = approvedCount - committedCount;

        return Ok(new CommitImportBatchResultDto(
            importBatch.Id,
            approvedCount,
            committedCount,
            skippedCount,
            importBatch.Status.ToString()));
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

    private static StagedTransactionDto ToDto(StagedTransaction staged)
    {
        return new StagedTransactionDto(
            staged.Id,
            staged.ImportBatchId,
            staged.RowIndex,
            staged.BookingDate,
            staged.ValueDate,
            staged.RawDescription,
            staged.Amount,
            staged.Currency,
            staged.RunningBalance,
            staged.IsApproved);
    }

    private static string BuildRowFingerprint(StagedTransaction staged)
    {
        var parts = new[]
        {
            staged.BookingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            staged.ValueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
            staged.RawDescription.Trim(),
            staged.Amount.ToString(CultureInfo.InvariantCulture),
            (staged.Currency ?? "EUR").Trim(),
            staged.RunningBalance?.ToString(CultureInfo.InvariantCulture) ?? string.Empty
        };

        var payload = string.Join("|", parts);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
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
