namespace FacilityApp.Data.Models;

public class RefreshToken
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public string   UserId    { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;
    public string   Token     { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public bool     IsRevoked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
