using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Services;

public class DocumentService : IDocumentService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly TenantContext _tenantCtx;
    private readonly IWebHostEnvironment _env;

    private static readonly HashSet<string> AllowedMime = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "image/jpeg", "image/png",
        "text/plain"
    };

    private const long MaxBytes = 10 * 1024 * 1024; // 10 MB

    public DocumentService(IDbContextFactory<AppDbContext> factory, TenantContext tenantCtx, IWebHostEnvironment env)
    {
        _factory   = factory;
        _tenantCtx = tenantCtx;
        _env       = env;
    }

    public async Task<List<Document>> GetAllAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Documents
           .Include(d => d.UploadedBy)
           .OrderByDescending(d => d.UploadedAt)
           .ToListAsync();
    }

    public async Task<List<Document>> GetActiveAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Documents
           .Include(d => d.UploadedBy)
           .Where(d => d.IsActive)
           .OrderByDescending(d => d.UploadedAt)
           .ToListAsync();
    }

    public async Task<Document> UploadAsync(string title, string? description, DocumentCategory category, IBrowserFile file, string uploadedById)
    {
        if (file.Size > MaxBytes)
            throw new InvalidOperationException("File size must not exceed 10 MB.");

        if (!AllowedMime.Contains(file.ContentType))
            throw new InvalidOperationException("File type not allowed. Upload PDF, Word, Excel, image, or plain text files.");

        var slug   = _tenantCtx.TenantSlug;
        var folder = Path.Combine(_env.WebRootPath, "documents", slug);
        Directory.CreateDirectory(folder);

        var ext      = Path.GetExtension(file.Name);
        var stored   = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(folder, stored);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.OpenReadStream(MaxBytes).CopyToAsync(stream);

        await using var db = await _factory.CreateDbContextAsync();
        var doc = new Document
        {
            TenantId         = _tenantCtx.TenantId,
            Title            = title.Trim(),
            Description      = description?.Trim(),
            Category         = category,
            OriginalFileName = file.Name,
            StoredFileName   = stored,
            FileSize         = file.Size,
            UploadedById     = uploadedById
        };
        db.Documents.Add(doc);
        await db.SaveChangesAsync();
        return doc;
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var d = await db.Documents.FindAsync(id)
            ?? throw new InvalidOperationException("Document not found.");
        d.IsActive = !d.IsActive;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var d = await db.Documents.FindAsync(id)
            ?? throw new InvalidOperationException("Document not found.");

        var fullPath = Path.Combine(_env.WebRootPath, "documents", _tenantCtx.TenantSlug, d.StoredFileName);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        db.Documents.Remove(d);
        await db.SaveChangesAsync();
    }
}
