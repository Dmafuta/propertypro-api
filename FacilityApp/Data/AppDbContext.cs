using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly TenantContext _tenantContext;

    public DbSet<Tenant>             Tenants             { get; set; }
    public DbSet<Unit>               Units               { get; set; }
    public DbSet<UnitType>           UnitTypes           { get; set; }
    public DbSet<UserUnit>           UserUnits           { get; set; }
    public DbSet<Meter>              Meters              { get; set; }
    public DbSet<MeterReading>       MeterReadings       { get; set; }
    public DbSet<PrepaidToken>       PrepaidTokens       { get; set; }
    public DbSet<MeterAlert>         MeterAlerts         { get; set; }
    public DbSet<Visitor>            Visitors            { get; set; }
    public DbSet<Visit>              Visits              { get; set; }
    public DbSet<Facility>           Facilities          { get; set; }
    public DbSet<AuditLog>           AuditLogs           { get; set; }
    public DbSet<AccessPass>         AccessPasses        { get; set; }
    public DbSet<BlacklistEntry>     BlacklistEntries    { get; set; }
    public DbSet<UnitRequest>        UnitRequests        { get; set; }
    public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }
    public DbSet<Announcement>       Announcements       { get; set; }
    public DbSet<Document>           Documents           { get; set; }
    public DbSet<IncidentReport>     IncidentReports     { get; set; }
    public DbSet<Entrance>           Entrances           { get; set; }
    public DbSet<Vehicle>            Vehicles            { get; set; }
    public DbSet<VehicleTag>         VehicleTags         { get; set; }
    public DbSet<ParkingRecord>      ParkingRecords      { get; set; }
    public DbSet<Parcel>             Parcels             { get; set; }
    public DbSet<RefreshToken>       RefreshTokens       { get; set; }
    public DbSet<EmployeeProfile>    EmployeeProfiles    { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options, TenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    // Exposed as a property so EF Core 9's ExpressionTreeFuncletizer treats it
    // as a context accessor (re-evaluated per query) rather than a captured closure.
    private Guid CurrentTenantId => _tenantContext.TenantId;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── Table names ────────────────────────────────────────────────────────
        builder.Entity<Tenant>().ToTable("tenants");
        builder.Entity<Unit>().ToTable("units");
        builder.Entity<UnitType>().ToTable("unit_types");
        builder.Entity<UserUnit>().ToTable("user_units");
        builder.Entity<Meter>().ToTable("meters");
        builder.Entity<MeterReading>().ToTable("meter_readings");
        builder.Entity<PrepaidToken>().ToTable("prepaid_tokens");
        builder.Entity<MeterAlert>().ToTable("meter_alerts");
        builder.Entity<Visitor>().ToTable("visitors");
        builder.Entity<Visit>().ToTable("visits");
        builder.Entity<Facility>().ToTable("facilities");
        builder.Entity<AuditLog>().ToTable("audit_logs");
        builder.Entity<AccessPass>().ToTable("access_passes");
        builder.Entity<BlacklistEntry>().ToTable("blacklist_entries");
        builder.Entity<UnitRequest>().ToTable("unit_requests");
        builder.Entity<MaintenanceRequest>().ToTable("maintenance_requests");
        builder.Entity<Announcement>().ToTable("announcements");
        builder.Entity<Document>().ToTable("documents");
        builder.Entity<IncidentReport>().ToTable("incident_reports");
        builder.Entity<Entrance>().ToTable("entrances");
        builder.Entity<Vehicle>().ToTable("vehicles");
        builder.Entity<VehicleTag>().ToTable("vehicle_tags");
        builder.Entity<ParkingRecord>().ToTable("parking_records");
        builder.Entity<Parcel>().ToTable("parcels");
        builder.Entity<RefreshToken>().ToTable("refresh_tokens");
        builder.Entity<EmployeeProfile>().ToTable("employee_profiles");

        // ── Indexes ────────────────────────────────────────────────────────────
        builder.Entity<Tenant>().HasIndex(t => t.Slug).IsUnique();

        // Prevent duplicate links (same user, unit, and link type)
        builder.Entity<UserUnit>()
            .HasIndex(uu => new { uu.UserId, uu.UnitId, uu.LinkType })
            .IsUnique();

        // ── Unit / UnitType ────────────────────────────────────────────────────
        builder.Entity<Unit>()
            .HasOne(u => u.UnitType)
            .WithMany(t => t.Units)
            .HasForeignKey(u => u.UnitTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── UserUnit relationships ──────────────────────────────────────────────
        builder.Entity<UserUnit>()
            .HasOne(uu => uu.User)
            .WithMany(u => u.UserUnits)
            .HasForeignKey(uu => uu.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserUnit>()
            .HasOne(uu => uu.Unit)
            .WithMany(u => u.UserUnits)
            .HasForeignKey(uu => uu.UnitId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Meter relationships ────────────────────────────────────────────────
        builder.Entity<Meter>()
            .HasOne(m => m.Unit)
            .WithMany(u => u.Meters)
            .HasForeignKey(m => m.UnitId)
            .OnDelete(DeleteBehavior.Cascade);

        // Self-referencing chain: PreviousMeter / ReplacedByMeter
        builder.Entity<Meter>()
            .HasOne(m => m.PreviousMeter)
            .WithOne(m => m.ReplacedByMeter)
            .HasForeignKey<Meter>(m => m.PreviousMeterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<MeterReading>()
            .HasOne(r => r.Meter)
            .WithMany(m => m.Readings)
            .HasForeignKey(r => r.MeterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<MeterReading>()
            .HasOne(r => r.ReadBy)
            .WithMany()
            .HasForeignKey(r => r.ReadByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<PrepaidToken>()
            .HasOne(t => t.Meter)
            .WithMany(m => m.PrepaidTokens)
            .HasForeignKey(t => t.MeterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PrepaidToken>()
            .HasOne(t => t.PurchasedBy)
            .WithMany()
            .HasForeignKey(t => t.PurchasedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<MeterAlert>()
            .HasOne(a => a.Meter)
            .WithMany(m => m.Alerts)
            .HasForeignKey(a => a.MeterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<MeterAlert>()
            .HasOne(a => a.AcknowledgedBy)
            .WithMany()
            .HasForeignKey(a => a.AcknowledgedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Multi-tenancy global query filters ─────────────────────────────────
        builder.Entity<Unit>().HasQueryFilter(u => u.TenantId == CurrentTenantId);
        builder.Entity<UnitType>().HasQueryFilter(t => t.TenantId == CurrentTenantId);
        builder.Entity<UserUnit>().HasQueryFilter(uu => uu.TenantId == CurrentTenantId);
        builder.Entity<Meter>().HasQueryFilter(m => m.TenantId == CurrentTenantId);
        builder.Entity<MeterReading>().HasQueryFilter(r => r.TenantId == CurrentTenantId);
        builder.Entity<PrepaidToken>().HasQueryFilter(t => t.TenantId == CurrentTenantId);
        builder.Entity<MeterAlert>().HasQueryFilter(a => a.TenantId == CurrentTenantId);
        builder.Entity<Visitor>().HasQueryFilter(v => v.TenantId == CurrentTenantId);
        builder.Entity<Visit>().HasQueryFilter(v => v.TenantId == CurrentTenantId);
        builder.Entity<Facility>().HasQueryFilter(f => f.TenantId == CurrentTenantId);
        builder.Entity<ApplicationUser>().HasQueryFilter(u => u.TenantId == CurrentTenantId);
        builder.Entity<AuditLog>().HasQueryFilter(a => a.TenantId == CurrentTenantId);
        builder.Entity<AccessPass>().HasQueryFilter(p => p.TenantId == CurrentTenantId);
        builder.Entity<BlacklistEntry>().HasQueryFilter(b => b.TenantId == CurrentTenantId);
        builder.Entity<UnitRequest>().HasQueryFilter(r => r.TenantId == CurrentTenantId);
        builder.Entity<MaintenanceRequest>().HasQueryFilter(m => m.TenantId == CurrentTenantId);
        builder.Entity<Announcement>().HasQueryFilter(a => a.TenantId == CurrentTenantId);
        builder.Entity<Document>().HasQueryFilter(d => d.TenantId == CurrentTenantId);
        builder.Entity<IncidentReport>().HasQueryFilter(i => i.TenantId == CurrentTenantId);
        builder.Entity<Entrance>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        builder.Entity<Vehicle>().HasQueryFilter(v => v.TenantId == CurrentTenantId);
        builder.Entity<VehicleTag>().HasQueryFilter(t => t.TenantId == CurrentTenantId);
        builder.Entity<ParkingRecord>().HasQueryFilter(p => p.TenantId == CurrentTenantId);
        builder.Entity<Parcel>().HasQueryFilter(p => p.TenantId == CurrentTenantId);
        builder.Entity<EmployeeProfile>().HasQueryFilter(e => e.TenantId == CurrentTenantId);

        // ── Other relationships ────────────────────────────────────────────────
        builder.Entity<UnitRequest>()
            .HasOne(r => r.Resident).WithMany()
            .HasForeignKey(r => r.ResidentId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<UnitRequest>()
            .HasOne(r => r.Unit).WithMany()
            .HasForeignKey(r => r.UnitId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<UnitRequest>()
            .HasOne(r => r.ReviewedBy).WithMany()
            .HasForeignKey(r => r.ReviewedById).OnDelete(DeleteBehavior.SetNull);

        builder.Entity<MaintenanceRequest>()
            .HasOne(m => m.Resident).WithMany()
            .HasForeignKey(m => m.ResidentId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<MaintenanceRequest>()
            .HasOne(m => m.Unit).WithMany()
            .HasForeignKey(m => m.UnitId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<MaintenanceRequest>()
            .HasOne(m => m.AssignedTo).WithMany()
            .HasForeignKey(m => m.AssignedToId).OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Announcement>()
            .HasOne(a => a.CreatedBy).WithMany()
            .HasForeignKey(a => a.CreatedById).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Document>()
            .HasOne(d => d.UploadedBy).WithMany()
            .HasForeignKey(d => d.UploadedById).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IncidentReport>()
            .HasOne(i => i.ReportedBy).WithMany()
            .HasForeignKey(i => i.ReportedById).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<IncidentReport>()
            .HasOne(i => i.ResolvedBy).WithMany()
            .HasForeignKey(i => i.ResolvedById).OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Visit>()
            .HasOne(v => v.EntryEntrance).WithMany()
            .HasForeignKey(v => v.EntryEntranceId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<Visit>()
            .HasOne(v => v.ExitEntrance).WithMany()
            .HasForeignKey(v => v.ExitEntranceId).OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Vehicle>()
            .HasOne(v => v.Owner).WithMany()
            .HasForeignKey(v => v.OwnerId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<VehicleTag>()
            .HasOne(t => t.Vehicle).WithOne(v => v.Tag)
            .HasForeignKey<VehicleTag>(t => t.VehicleId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<VehicleTag>()
            .HasOne(t => t.IssuedBy).WithMany()
            .HasForeignKey(t => t.IssuedById).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<VehicleTag>()
            .HasIndex(t => new { t.TenantId, t.TagNumber }).IsUnique();

        builder.Entity<ParkingRecord>()
            .HasOne(p => p.Vehicle).WithMany()
            .HasForeignKey(p => p.VehicleId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<ParkingRecord>()
            .HasOne(p => p.VehicleTag).WithMany()
            .HasForeignKey(p => p.VehicleTagId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<ParkingRecord>()
            .HasOne(p => p.Visit).WithMany()
            .HasForeignKey(p => p.VisitId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<ParkingRecord>()
            .HasOne(p => p.EntryEntrance).WithMany()
            .HasForeignKey(p => p.EntryEntranceId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<ParkingRecord>()
            .HasOne(p => p.ExitEntrance).WithMany()
            .HasForeignKey(p => p.ExitEntranceId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<ParkingRecord>()
            .HasOne(p => p.LoggedBy).WithMany()
            .HasForeignKey(p => p.LoggedById).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Parcel>()
            .HasOne(p => p.Unit).WithMany()
            .HasForeignKey(p => p.UnitId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<Parcel>()
            .HasOne(p => p.ReceivedBy).WithMany()
            .HasForeignKey(p => p.ReceivedById).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<EmployeeProfile>()
            .HasOne(e => e.User).WithOne()
            .HasForeignKey<EmployeeProfile>(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<EmployeeProfile>()
            .HasIndex(e => new { e.TenantId, e.UserId }).IsUnique();

        builder.Entity<RefreshToken>()
            .HasOne(r => r.User).WithMany()
            .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<RefreshToken>()
            .HasIndex(r => r.Token).IsUnique();
        // No global query filter on RefreshTokens — looked up by token value only
    }
}
