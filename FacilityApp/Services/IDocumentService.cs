using FacilityApp.Data.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace FacilityApp.Services;

public interface IDocumentService
{
    Task<List<Document>> GetAllAsync();
    Task<List<Document>> GetActiveAsync();
    Task<Document> UploadAsync(string title, string? description, DocumentCategory category, IBrowserFile file, string uploadedById);
    Task ToggleActiveAsync(Guid id);
    Task DeleteAsync(Guid id);
}
