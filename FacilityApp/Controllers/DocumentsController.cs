using FacilityApp.Data;
using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class DocumentsController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly TenantContext                   _tenantCtx;
    private readonly IWebHostEnvironment             _env;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public DocumentsController(
        IDbContextFactory<AppDbContext> factory,
        TenantContext tenantCtx,
        IWebHostEnvironment env)
    {
        _factory   = factory;
        _tenantCtx = tenantCtx;
        _env       = env;
    }

    // GET /api/documents          (admin — all)
    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Manager,Admin")]
    public async Task<IActionResult> GetAll()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var items = await db.Documents.OrderByDescending(d => d.UploadedAt).ToListAsync();
        return Ok(items);
    }

    // GET /api/documents/active   (residents — active only)
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var items = await db.Documents
            .Where(d => d.IsActive)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
        return Ok(items);
    }

    // POST /api/documents  (multipart form upload)
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Manager,Admin")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> Upload(
        [FromForm] string title,
        [FromForm] string? description,
        [FromForm] int category,
        IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        var allowedTypes = new[] { "application/pdf", "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };

        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest(new { error = "Only PDF and Word documents are allowed." });

        var slug   = _tenantCtx.TenantSlug;
        var dir    = Path.Combine(_env.WebRootPath, "documents", slug);
        Directory.CreateDirectory(dir);

        var ext      = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(dir, fileName);

        await using (var stream = System.IO.File.Create(filePath))
            await file.CopyToAsync(stream);

        await using var db = await _factory.CreateDbContextAsync();
        var doc = new Document
        {
            TenantId         = _tenantCtx.TenantId,
            Title            = title.Trim(),
            Description      = description?.Trim(),
            Category         = (DocumentCategory)category,
            StoredFileName   = fileName,
            OriginalFileName = file.FileName,
            FileSize         = file.Length,
            UploadedById     = UserId,
            IsActive         = true
        };
        db.Documents.Add(doc);
        await db.SaveChangesAsync();
        return Ok(doc);
    }

    // PATCH /api/documents/{id}/toggle
    [HttpPatch("{id:guid}/toggle")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Manager,Admin")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var doc = await db.Documents.FindAsync(id);
        if (doc is null) return NotFound();
        doc.IsActive = !doc.IsActive;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/documents/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var doc = await db.Documents.FindAsync(id);
        if (doc is null) return NotFound();

        // Remove file from disk
        var filePath = Path.Combine(_env.WebRootPath, "documents", _tenantCtx.TenantSlug, doc.StoredFileName);
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);

        db.Documents.Remove(doc);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
