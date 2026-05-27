using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Services;

public class VisitorService : IVisitorService
{
    private readonly AppDbContext _context;
    private readonly TenantContext _tenantCtx;
    private readonly IEmailService _email;
    private readonly ISmsService _sms;
    private readonly IAuditService _audit;
    private readonly IBlacklistService _blacklist;

    public VisitorService(AppDbContext context, TenantContext tenantCtx,
        IEmailService email, ISmsService sms, IAuditService audit,
        IBlacklistService blacklist)
    {
        _context   = context;
        _tenantCtx = tenantCtx;
        _email     = email;
        _sms       = sms;
        _audit     = audit;
        _blacklist = blacklist;
    }

    public async Task<(List<Visit> Items, int Total)> GetVisitsAsync(
        string tab, string? search, int page = 1, int pageSize = 25)
    {
        var now      = DateTime.UtcNow;
        var today    = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var tomorrow = today.AddDays(1);

        var q = _context.Visits
            .Include(v => v.Visitor)
            .Include(v => v.Host)
            .Include(v => v.EntryEntrance)
            .Include(v => v.ExitEntrance)
            .AsQueryable();

        q = tab switch
        {
            "today"    => q.Where(v => (v.ScheduledAt >= today && v.ScheduledAt < tomorrow)
                                    || (v.CheckedInAt != null && v.CheckedInAt >= today && v.CheckedInAt < tomorrow)),
            "upcoming" => q.Where(v => v.Status == VisitStatus.Scheduled && v.ScheduledAt >= now),
            "active"   => q.Where(v => v.Status == VisitStatus.CheckedIn),
            "history"  => q.Where(v => v.Status == VisitStatus.CheckedOut
                                    || v.Status == VisitStatus.Cancelled
                                    || v.Status == VisitStatus.NoShow),
            _          => q
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(v =>
                v.Visitor.FullName.ToLower().Contains(s) ||
                v.Visitor.Email.ToLower().Contains(s)    ||
                v.Visitor.Phone.Contains(s)              ||
                (v.Visitor.Company != null && v.Visitor.Company.ToLower().Contains(s)) ||
                v.Purpose.ToLower().Contains(s));
        }

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(v => v.ScheduledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<List<Visit>> GetScheduledVisitsAsync(string? search)
    {
        var q = _context.Visits
            .Include(v => v.Visitor)
            .Include(v => v.Host)
            .Where(v => v.Status == VisitStatus.Scheduled)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(v =>
                v.Visitor.FullName.ToLower().Contains(s) ||
                v.Visitor.Phone.Contains(s)              ||
                v.Visitor.Email.ToLower().Contains(s));
        }

        return await q
            .OrderBy(v => v.ScheduledAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task<List<Visit>> GetVisitsForHostAsync(string hostUserId)
        => await _context.Visits
            .Include(v => v.Visitor)
            .Include(v => v.EntryEntrance)
            .Include(v => v.ExitEntrance)
            .Where(v => v.HostUserId == hostUserId)
            .OrderByDescending(v => v.ScheduledAt)
            .Take(200)
            .ToListAsync();

    public async Task MarkNoShowAsync(Guid visitId)
    {
        var visit = await _context.Visits.FindAsync(visitId)
            ?? throw new InvalidOperationException("Visit not found.");
        visit.Status = VisitStatus.NoShow;
        await _context.SaveChangesAsync();
    }

    public async Task<Visit> WalkInAsync(
        string fullName, string email, string phone, string? company,
        string purpose, string? hostUserId, string? notes = null, string? photoUrl = null,
        Guid? entranceId = null)
    {
        var blocked = await _blacklist.CheckAsync(email, phone, entranceId);
        if (blocked is not null && blocked.EntryType == Data.Models.BlacklistType.Blacklisted)
            throw new VisitorBlockedException(blocked);

        var visitor = await GetOrCreateVisitorAsync(fullName, email, phone, company);

        // Append watchlist warning to notes
        if (blocked is not null)
            notes = string.IsNullOrWhiteSpace(notes)
                ? $"⚠ WATCHLIST: {blocked.Reason}"
                : $"{notes} | ⚠ WATCHLIST: {blocked.Reason}";
        var now     = DateTime.UtcNow;

        // Update photo if provided
        if (photoUrl is not null) { visitor.PhotoUrl = photoUrl; }

        var visit = new Visit
        {
            TenantId         = _tenantCtx.TenantId,
            VisitorId        = visitor.Id,
            HostUserId       = hostUserId,
            Purpose          = purpose.Trim(),
            ScheduledAt      = now,
            CheckedInAt      = now,
            Status           = VisitStatus.CheckedIn,
            Notes            = notes?.Trim(),
            EntryEntranceId  = entranceId
        };
        _context.Visits.Add(visit);
        await _context.SaveChangesAsync();

        var walkInGate = await EntranceNameAsync(entranceId);
        await _audit.LogAsync("WalkIn", "Visit", visit.Id.ToString(),
            $"{visitor.FullName} walked in{walkInGate} — purpose: {purpose}");

        // Notify host if they have an email or phone
        if (hostUserId is not null)
        {
            var host = await _context.Users.FindAsync(hostUserId);
            if (host?.Email is not null)
                _ = _email.SendCheckInAlertAsync(host.Email, host.FullName, visitor.FullName, purpose, _tenantCtx.TenantName);
            if (host?.PhoneNumber is not null)
                _ = _sms.SendCheckInAlertAsync(host.PhoneNumber, host.FullName, visitor.FullName, purpose, _tenantCtx.TenantName);
        }

        return visit;
    }

    public async Task<Visit> PreRegisterAsync(
        string fullName, string email, string phone, string? company,
        string purpose, string? hostUserId, DateTime scheduledAt, string? notes = null)
    {
        var visitor = await GetOrCreateVisitorAsync(fullName, email, phone, company);

        var visit = new Visit
        {
            TenantId    = _tenantCtx.TenantId,
            VisitorId   = visitor.Id,
            HostUserId  = hostUserId,
            Purpose     = purpose.Trim(),
            ScheduledAt = scheduledAt.Kind == DateTimeKind.Utc
                            ? scheduledAt
                            : scheduledAt.ToUniversalTime(),
            Status      = VisitStatus.Scheduled,
            Notes       = notes?.Trim()
        };
        _context.Visits.Add(visit);
        await _context.SaveChangesAsync();

        await _audit.LogAsync("PreRegister", "Visit", visit.Id.ToString(),
            $"{visitor.FullName} pre-registered — purpose: {purpose}, scheduled: {scheduledAt:u}");

        // Confirm to host
        if (hostUserId is not null)
        {
            var host = await _context.Users.FindAsync(hostUserId);
            if (host?.Email is not null)
                _ = _email.SendVisitConfirmationAsync(host.Email, host.FullName, visitor.FullName, purpose, scheduledAt, _tenantCtx.TenantName);
            if (host?.PhoneNumber is not null)
                _ = _sms.SendVisitConfirmationAsync(host.PhoneNumber, host.FullName, visitor.FullName, purpose, scheduledAt, _tenantCtx.TenantName);
        }

        return visit;
    }

    public async Task CheckInAsync(Guid visitId, Guid? entranceId = null)
    {
        var visit = await _context.Visits
            .Include(v => v.Visitor)
            .Include(v => v.Host)
            .FirstOrDefaultAsync(v => v.Id == visitId)
            ?? throw new InvalidOperationException("Visit not found.");

        var blocked = await _blacklist.CheckAsync(visit.Visitor.Email, visit.Visitor.Phone, entranceId);
        if (blocked is not null && blocked.EntryType == Data.Models.BlacklistType.Blacklisted)
            throw new VisitorBlockedException(blocked);

        visit.Status         = VisitStatus.CheckedIn;
        visit.CheckedInAt    = DateTime.UtcNow;
        visit.EntryEntranceId = entranceId;
        await _context.SaveChangesAsync();

        var checkInGate = await EntranceNameAsync(entranceId);
        await _audit.LogAsync("CheckIn", "Visit", visitId.ToString(),
            $"{visit.Visitor.FullName} checked in{checkInGate}");

        if (visit.Host?.Email is not null)
            _ = _email.SendCheckInAlertAsync(visit.Host.Email, visit.Host.FullName, visit.Visitor.FullName, visit.Purpose, _tenantCtx.TenantName);
        if (visit.Host?.PhoneNumber is not null)
            _ = _sms.SendCheckInAlertAsync(visit.Host.PhoneNumber, visit.Host.FullName, visit.Visitor.FullName, visit.Purpose, _tenantCtx.TenantName);
    }

    public async Task CheckOutAsync(Guid visitId, Guid? entranceId = null)
    {
        var visit = await _context.Visits
            .Include(v => v.Visitor)
            .FirstOrDefaultAsync(v => v.Id == visitId)
            ?? throw new InvalidOperationException("Visit not found.");

        visit.Status          = VisitStatus.CheckedOut;
        visit.CheckedOutAt    = DateTime.UtcNow;
        visit.ExitEntranceId  = entranceId;
        await _context.SaveChangesAsync();

        var checkOutGate = await EntranceNameAsync(entranceId);
        await _audit.LogAsync("CheckOut", "Visit", visitId.ToString(),
            $"{visit.Visitor.FullName} checked out{checkOutGate}");
    }

    public async Task CancelAsync(Guid visitId)
    {
        var visit = await _context.Visits.FindAsync(visitId)
            ?? throw new InvalidOperationException("Visit not found.");
        visit.Status = VisitStatus.Cancelled;
        await _context.SaveChangesAsync();

        await _audit.LogAsync("Cancel", "Visit", visitId.ToString(), null);
    }

    public async Task<List<ApplicationUser>> GetHostsAsync()
    {
        return await _context.Users
            .Where(u => u.UserType == UserType.Staff)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    private async Task<string> EntranceNameAsync(Guid? entranceId)
    {
        if (entranceId is null) return "";
        var entrance = await _context.Entrances.FindAsync(entranceId.Value);
        return entrance is not null ? $" via {entrance.Name}" : "";
    }

    private async Task<Visitor> GetOrCreateVisitorAsync(
        string fullName, string email, string phone, string? company)
    {
        var emailNorm = email.Trim().ToLower();
        var existing  = await _context.Visitors
            .FirstOrDefaultAsync(v => v.Email == emailNorm);

        if (existing is not null)
        {
            existing.FullName = fullName.Trim();
            existing.Phone    = phone.Trim();
            existing.Company  = company?.Trim();
            await _context.SaveChangesAsync();
            return existing;
        }

        var visitor = new Visitor
        {
            TenantId = _tenantCtx.TenantId,
            FullName = fullName.Trim(),
            Email    = emailNorm,
            Phone    = phone.Trim(),
            Company  = company?.Trim()
        };
        _context.Visitors.Add(visitor);
        await _context.SaveChangesAsync();
        return visitor;
    }
}
