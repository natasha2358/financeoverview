using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace FinanceOverview.Api.Services;

public sealed record StoredImportFile(string StorageKey, string FullPath, string Sha256Hash);

public sealed class ImportStorageService
{
    private const string UploadsFolderName = "uploads";
    private readonly string _uploadsRoot;

    public ImportStorageService(IWebHostEnvironment environment)
    {
        _uploadsRoot = Path.Combine(environment.ContentRootPath, "App_Data", UploadsFolderName);
        Directory.CreateDirectory(_uploadsRoot);
    }

    public async Task<StoredImportFile> SavePdfAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var storageFileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var fullPath = Path.Combine(_uploadsRoot, storageFileName);
        var storageKey = Path.Combine("App_Data", UploadsFolderName, storageFileName);

        using var sha256 = SHA256.Create();
        await using var sourceStream = file.OpenReadStream();
        await using var destinationStream = File.Create(fullPath);

        var buffer = new byte[81920];
        int bytesRead;

        while ((bytesRead = await sourceStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await destinationStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        var hash = Convert.ToHexString(sha256.Hash ?? Array.Empty<byte>()).ToLowerInvariant();

        return new StoredImportFile(storageKey, fullPath, hash);
    }
}
