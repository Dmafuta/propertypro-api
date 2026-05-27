namespace FacilityApp.Controllers;

// ── Auth ─────────────────────────────────────────────────────────────────────
public record LoginRequest(string Slug, string Email, string Password);
public record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserDto User);
public record UserDto(string Id, string Name, string Email, string[] Roles, string TenantSlug, string TenantName, string UserType);
public record RefreshRequest(string RefreshToken);
public record RefreshResponse(string AccessToken, DateTime ExpiresAt);
public record LogoutRequest(string RefreshToken);
public record ForgotPasswordRequest(string Slug, string Email);
public record ResetPasswordRequest(string Slug, string Email, string Token, string NewPassword);

// ── SuperAdmin ────────────────────────────────────────────────────────────────
public record TenantDto(Guid Id, string Name, string Slug, bool IsActive, int Plan,
    string? CustomDomain, string? ContactEmail, string? ContactPhone,
    string? Address, string? Website, string? PrimaryColour, string? LogoUrl, DateTime CreatedAt);
public record CreateTenantRequest(string Name, string Slug, string ContactEmail);
public record UpdatePlanRequest(int Plan);

// ── Dashboard ─────────────────────────────────────────────────────────────────
public record DashboardResponse(int TodayVisitors, int ActiveVisitors, int PendingParcels,
    int OpenMaintenance, int TotalUnits, int OccupiedUnits);

// ── Visitors / Visits ─────────────────────────────────────────────────────────
public record WalkInRequest(
    string  FullName, string Email, string Phone, string? Company,
    string  Purpose, string? HostUserId, string? Notes, Guid? EntranceId);

public record PreRegisterRequest(
    string   FullName, string Email, string Phone, string? Company,
    string   Purpose, string? HostUserId, DateTime ScheduledAt, string? Notes);

public record CheckInRequest(Guid? EntranceId);
public record CheckOutRequest(Guid? EntranceId);

// ── Parcels ───────────────────────────────────────────────────────────────────
public record ReceiveParcelRequest(
    Guid?   UnitId, string RecipientName, string? CourierName,
    string  Description, string? Notes);

public record CollectParcelRequest(string CollectedByName);

// ── Maintenance ───────────────────────────────────────────────────────────────
public record SubmitMaintenanceRequest(
    Guid?  UnitId, string Title, string Description, int Category, int Priority);

public record UpdateMaintenanceRequest(int Status, string? StaffNote);

// ── Units ─────────────────────────────────────────────────────────────────────
public record CreateUnitRequest(string UnitNumber, string? Block, string? Floor, string? Description);
public record UpdateUnitRequest(string UnitNumber, string? Block, string? Floor, string? Description);
public record AssignUserRequest(string UserId);

// ── Users ─────────────────────────────────────────────────────────────────────
public record CreateStaffRequest(string FullName, string Email, string Password, string Role);
public record UpdateUserRoleRequest(string Role);
public record UpdatePhoneRequest(string? PhoneNumber);

// ── Parking ───────────────────────────────────────────────────────────────────
public record RegisterVehicleRequest(
    string OwnerId, string Plate, string Make, string Model,
    string Colour, int Type, string? Notes);

public record IssueTagRequest(Guid VehicleId, DateTime? ExpiresAt, string? Notes);
public record UpdateTagStatusRequest(int Status);
public record LogEntryByTagRequest(string TagNumber, Guid? EntranceId);
public record LogVisitorEntryRequest(string Plate, Guid? VisitId, Guid? EntranceId, string? Notes);
public record LogExitRequest(Guid? ExitEntranceId);

// ── Incidents ─────────────────────────────────────────────────────────────────
public record CreateIncidentRequest(
    string Title, string Description, string Location,
    string? InvolvedParties, int Category, int Severity);

public record UpdateIncidentRequest(int Status, string? ResolutionNotes);

// ── Settings ──────────────────────────────────────────────────────────────────
public record UpdateSettingsRequest(
    string  Name, string? ContactEmail, string? ContactPhone,
    string? Address, string? Website, string? CustomDomain);

public record UpdateBrandingRequest(string? LogoUrl, string? PrimaryColour);

// ── Announcements ─────────────────────────────────────────────────────────────
public record CreateAnnouncementRequest(string Title, string Body, int Category, DateTime? ExpiresAt);

// ── Entrances ─────────────────────────────────────────────────────────────────
public record CreateEntranceRequest(string Name, string? Description);
public record UpdateEntranceRequest(string Name, string? Description);
public record SetEntranceRequest(Guid EntranceId);
