using FacilityApp.Controllers;
using Microsoft.AspNetCore.Http;

namespace FacilityApp.Services;

public interface IAccountService
{
    Task<UserProfileDto?> GetProfileAsync(string userId);
    Task UpdateProfileAsync(string userId, string firstName, string? middleName, string lastName);
    Task<string?> UpdateUsernameAsync(string userId, string userName);
    Task<string?> UpdateEmailAsync(string userId, string primaryEmail, string? secondaryEmail);
    Task<string> UploadAvatarAsync(string userId, string slug, IFormFile file);
}
