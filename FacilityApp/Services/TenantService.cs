using FacilityApp.Data.Models;
using Npgsql;

namespace FacilityApp.Services;

public class TenantService : ITenantService
{
    private readonly NpgsqlDataSource _dataSource;

    public TenantService(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<Tenant?> ResolveBySlugAsync(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT "Id","Name","Slug","CustomDomain","IsActive","LogoUrl","PrimaryColour",
                   "ContactEmail","ContactPhone","Address","Website","CreatedAt","Plan","IsSystem"
            FROM tenants
            WHERE "Slug" = @slug AND "IsActive" = true
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("slug", slug.ToLower());
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapTenant(reader) : null;
    }

    public async Task<Tenant?> ResolveByDomainAsync(string? host)
    {
        if (string.IsNullOrWhiteSpace(host)) return null;
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT "Id","Name","Slug","CustomDomain","IsActive","LogoUrl","PrimaryColour",
                   "ContactEmail","ContactPhone","Address","Website","CreatedAt","Plan","IsSystem"
            FROM tenants
            WHERE "CustomDomain" = @host AND "IsActive" = true
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("host", host.ToLower());
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapTenant(reader) : null;
    }

    public async Task<Tenant?> ResolveByIdAsync(Guid id)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd  = conn.CreateCommand();
        cmd.CommandText = """
            SELECT "Id","Name","Slug","CustomDomain","IsActive","LogoUrl","PrimaryColour",
                   "ContactEmail","ContactPhone","Address","Website","CreatedAt","Plan","IsSystem"
            FROM tenants
            WHERE "Id" = @id
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapTenant(reader) : null;
    }

    private static Tenant MapTenant(NpgsqlDataReader r) => new()
    {
        Id            = r.GetGuid(0),
        Name          = r.GetString(1),
        Slug          = r.GetString(2),
        CustomDomain  = r.IsDBNull(3) ? null : r.GetString(3),
        IsActive      = r.GetBoolean(4),
        LogoUrl       = r.IsDBNull(5) ? null : r.GetString(5),
        PrimaryColour = r.IsDBNull(6) ? null : r.GetString(6),
        ContactEmail  = r.IsDBNull(7) ? null : r.GetString(7),
        ContactPhone  = r.IsDBNull(8) ? null : r.GetString(8),
        Address       = r.IsDBNull(9) ? null : r.GetString(9),
        Website       = r.IsDBNull(10) ? null : r.GetString(10),
        CreatedAt     = r.GetDateTime(11),
        Plan          = (TenantPlan)r.GetInt32(12),
        IsSystem      = r.GetBoolean(13),
    };
}
