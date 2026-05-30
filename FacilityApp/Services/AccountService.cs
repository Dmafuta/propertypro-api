using FacilityApp.Controllers;
using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Services;

public class AccountService : IAccountService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly AppDbContext                 _db;
    private readonly IWebHostEnvironment          _env;

    private static readonly string[] AllowedMimeTypes =
        ["image/jpeg", "image/png", "image/gif", "image/webp"];

    public AccountService(UserManager<ApplicationUser> users, AppDbContext db, IWebHostEnvironment env)
    {
        _users = users;
        _db    = db;
        _env   = env;
    }

    // Bypass the global TenantId query filter — account endpoints must work for all
    // users including SuperAdmin (whose TenantId differs from the resolved tenant context).
    private Task<ApplicationUser?> FindUserAsync(string userId) =>
        _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);

    public async Task<UserProfileDto?> GetProfileAsync(string userId)
    {
        var user = await FindUserAsync(userId);
        if (user is null) return null;
        var roles = await _users.GetRolesAsync(user);
        return new UserProfileDto(
            user.Id, user.FirstName, user.MiddleName, user.LastName,
            user.UserName ?? "", user.Email ?? "", user.SecondaryEmail,
            user.PhoneNumber, user.PhoneNumberConfirmed, user.AvatarUrl,
            roles.ToArray());
    }

    public async Task UpdateProfileAsync(string userId, string firstName, string? middleName, string lastName)
    {
        var user = await FindUserAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        user.FirstName  = firstName.Trim();
        user.MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim();
        user.LastName   = lastName.Trim();

        await _users.UpdateAsync(user);
    }

    public async Task<string?> UpdateUsernameAsync(string userId, string userName)
    {
        var user = await FindUserAsync(userId);
        if (user is null) return "User not found.";

        var result = await _users.SetUserNameAsync(user, userName.Trim());
        return result.Succeeded ? null : result.Errors.FirstOrDefault()?.Description ?? "Failed to update username.";
    }

    public async Task<string?> UpdateEmailAsync(string userId, string primaryEmail, string? secondaryEmail)
    {
        var user = await FindUserAsync(userId);
        if (user is null) return "User not found.";

        var result = await _users.SetEmailAsync(user, primaryEmail.Trim().ToLower());
        if (!result.Succeeded)
            return result.Errors.FirstOrDefault()?.Description ?? "Failed to update email.";

        user.SecondaryEmail = string.IsNullOrWhiteSpace(secondaryEmail)
            ? null
            : secondaryEmail.Trim().ToLower();

        await _users.UpdateAsync(user);
        return null;
    }

    public async Task<string> UploadAvatarAsync(string userId, string slug, IFormFile file)
    {
        if (!AllowedMimeTypes.Contains(file.ContentType.ToLower()))
            throw new InvalidOperationException("Only JPEG, PNG, GIF, and WebP images are allowed.");

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("File must be under 5 MB.");

        var user = await FindUserAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        var ext      = Path.GetExtension(file.FileName).ToLowerInvariant();
        var dir      = Path.Combine(_env.WebRootPath, "uploads", slug, "avatars");
        Directory.CreateDirectory(dir);

        var fileName = $"{userId}{ext}";
        var filePath = Path.Combine(dir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var url      = $"/uploads/{slug}/avatars/{fileName}";
        user.AvatarUrl = url;
        await _users.UpdateAsync(user);

        return url;
    }
}
