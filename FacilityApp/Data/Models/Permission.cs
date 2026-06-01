namespace FacilityApp.Data.Models;

/// <summary>
/// Granular permission flags managed by SuperAdmin and enforced at runtime.
/// Each value is stored as an integer (one row per permission per role).
/// </summary>
public enum Permission
{
    // Access control
    CanCheckInVisitors     = 1,
    CanPreRegisterVisits   = 2,
    CanManageVisitors      = 3,
    CanManageAccess        = 4,   // Passes, Blacklist

    // Reporting
    CanViewReports         = 5,
    CanViewDashboard       = 6,
    CanManageAuditLog      = 7,

    // Admin
    CanManageUsers         = 8,
    CanManageUnits         = 9,
    CanManageSettings      = 10,
    CanManageFacilities    = 11,
    CanManageEntrances     = 12,

    // Operations
    CanLogIncidents        = 13,
    CanManageIncidents     = 14,
    CanAccessParking       = 15,
    CanManageParking       = 16,
    CanManageParcels       = 17,
    CanManageMaintenance   = 18,
    CanManagePayments      = 19,

    // Communication
    CanManageDocuments     = 20,
    CanManageAnnouncements = 21,

    // HR
    CanManageHr            = 22,
}
