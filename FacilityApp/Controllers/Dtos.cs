namespace FacilityApp.Controllers;

// ── Auth ─────────────────────────────────────────────────────────────────────
public record LoginRequest(string Slug, string Email, string Password);
public record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserDto User);
public record UserDto(string Id, string FullName, string Email, string[] Roles,
    string TenantId, string TenantSlug, string TenantName, string UserType,
    string? PrimaryColour = null, string? LogoUrl = null);
public record RefreshRequest(string RefreshToken);
public record RefreshResponse(string AccessToken, DateTime ExpiresAt);
public record LogoutRequest(string RefreshToken);
public record ForgotPasswordRequest(string Slug, string Email);
public record ResetPasswordRequest(string Slug, string Email, string Token, string NewPassword);

// ── SuperAdmin ────────────────────────────────────────────────────────────────
public record TwoFactorRequiredResponse(bool RequiresTwoFactor, string TempToken, string MaskedPhone);
public record SuperAdminVerify2FaRequest(string TempToken, string Code);
public record SuperAdminResend2FaRequest(string TempToken);

public record TenantDto(Guid Id, string Name, string Slug, bool IsActive, int Plan,
    string? CustomDomain, string? ContactEmail, string? ContactPhone,
    string? Address, string? Website, string? PrimaryColour, string? LogoUrl, DateTime CreatedAt);
public record CreateTenantRequest(string Name, string Slug, string ContactEmail);
public record SeedAdminRequest(string FirstName, string LastName, string Email);
public record UpdatePlanRequest(int Plan);
public record UpdateSmsRequest(bool Enabled, string? ApiKey, string? Username, string? SenderId);

public record TenantHealthDto(
    int TotalStaff, int TotalResidents,
    int VisitorVolume30d, int MaintenanceBacklog,
    int TotalUnits, int OccupiedUnits, int OpenIncidents);

// ── Tenant (public) ───────────────────────────────────────────────────────────
public record TenantPublicDto(
    string Id, string Name, string Slug, string Plan,
    string? LogoUrl, string? PrimaryColour);

// ── Dashboard ─────────────────────────────────────────────────────────────────
public record UpcomingVisitDto(Guid Id, string VisitorName, string Purpose, DateTime ScheduledAt, string? HostName);

public record DashboardResponse(
    int TodayVisits, int ActiveVisits, int PendingParcels,
    int OpenMaintenance, int TotalUnits, int OccupiedUnits,
    int OpenIncidents, int PendingUnitRequests, int ActiveVehicles,
    List<UpcomingVisitDto> UpcomingVisits);

// ── Resident API ──────────────────────────────────────────────────────────────
public record ResidentVisitDto(
    Guid Id, string VisitorName, string? VisitorPhone, string Purpose,
    string Status, DateTime ScheduledAt, DateTime? CheckedInAt, DateTime? CheckedOutAt,
    string? QrCode);

public record ResidentUpcomingVisitDto(Guid Id, string VisitorName, string Purpose, DateTime ScheduledAt);

public record ResidentDashboardResponse(
    int UpcomingVisits, int ActiveVisits, int TotalVisits,
    int PendingParcels, int OpenMaintenance,
    bool HasUnit, string? UnitNumber, bool PendingUnitRequest,
    List<ResidentUpcomingVisitDto> UpcomingVisitsList);

public record ResidentDocumentDto(Guid Id, string Title, string Category, string FileUrl, DateTime UploadedAt);

public record ResidentAnnouncementDto(
    Guid Id, string Title, string Body, string Category,
    DateTime PublishedAt, DateTime? ExpiresAt);

public record ResidentUnitRequestDto(
    Guid Id, Guid UnitId, string UnitNumber, string Status,
    string? Note, string? ReviewNote, DateTime RequestedAt, DateTime? ReviewedAt);

public record SubmitUnitRequestRequest(Guid UnitId, string? Note);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record ResidentProfileDto(string Id, string FullName, string Email, string? PhoneNumber, string? UnitNumber);
public record UpdateResidentProfileRequest(string? FullName, string? PhoneNumber);

public record ResidentPreRegisterRequest(
    string VisitorName, string VisitorPhone, string? VisitorEmail, string Purpose, DateTime ScheduledAt);

public record ResidentVehicleInput(string LicensePlate, string? Make, string? Model, string? Colour);

public record ResidentVehicleDto(
    Guid Id, string LicensePlate, string? Make, string? Model, string? Colour,
    string? TagNumber, string? TagStatus);

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
public record CreateStaffRequest(string FullName, string Email, string? PhoneNumber);
public record UpdateUserRoleRequest(string? Role);
public record UpdatePhoneRequest(string? PhoneNumber);

// ── Account (self-service profile) ────────────────────────────────────────────
public record UserProfileDto(
    string Id, string FirstName, string? MiddleName, string LastName,
    string UserName, string Email, string? SecondaryEmail,
    string? PhoneNumber, bool PhoneNumberConfirmed, string? AvatarUrl, string[] Roles);

public record UpdateProfileRequest(string FirstName, string? MiddleName, string LastName);
public record UpdateUsernameRequest(string UserName);
public record UpdateEmailRequest(string PrimaryEmail, string? SecondaryEmail);
public record AvatarUploadResponse(string AvatarUrl);
public record PhoneStatusDto(string? PhoneNumber, bool PhoneNumberConfirmed, bool TwoFactorEnabled);
public record SendPhoneVerificationRequest(string PhoneNumber);
public record SendPhoneVerificationResponse(string MaskedPhone);
public record VerifyPhoneRequest(string PhoneNumber, string Code);

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

// ── Badge ─────────────────────────────────────────────────────────────────────
public record BadgeDto(
    Guid VisitId,
    string VisitorName, string VisitorEmail, string VisitorPhone, string? Company,
    string? PhotoUrl, string Purpose, string? HostName,
    DateTime ScheduledAt, DateTime? CheckedInAt,
    int Status, string? EntryEntrance,
    string TenantName, string? TenantLogoUrl, string? TenantPrimaryColour);

public record SendBadgeRequest(string? CustomEmail);

// ── HR ────────────────────────────────────────────────────────────────────────
public record HrStaffDto(
    string Id, string FullName, string Email, string? PhoneNumber,
    string[] Roles, string? Department, string? ContractType,
    DateTime? JoiningDate, bool IsActive, DateTime CreatedAt,
    EmployeeProfileDto? Profile);

public record EmployeeProfileDto(
    string? MiddleName, string? NationalId, string? PassportNumber,
    DateTime? DateOfBirth, string? Gender, string? Address,
    DateTime? JoiningDate, string? ContractType, string? Department,
    string? EmergencyContactName, string? EmergencyContactPhone);

public record UpsertEmployeeProfileRequest(
    string? MiddleName, string? NationalId, string? PassportNumber,
    DateTime? DateOfBirth, string? Gender, string? Address,
    DateTime? JoiningDate, string? ContractType, string? Department,
    string? EmergencyContactName, string? EmergencyContactPhone);

// ── Entrances ─────────────────────────────────────────────────────────────────
public record CreateEntranceRequest(string Name, string? Description);
public record UpdateEntranceRequest(string Name, string? Description);
public record SetEntranceRequest(Guid EntranceId);
