using System.Security.Claims;
using System.Text;
using FacilityApp.Data;
using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace FacilityApp
{
    public class Program
    {
        // Staff sub-roles
        public const string RoleAdmin        = "Admin";
        public const string RoleManager      = "Manager";
        public const string RoleReceptionist = "Receptionist";
        public const string RoleSecurity     = "Security";

        // Granted to any user who physically occupies a unit
        public const string RoleOccupant = "Occupant";

        // Cross-tenant super administrator
        public const string RoleSuperAdmin = "SuperAdmin";

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connStr))
                throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' is not configured. " +
                    "Set the environment variable: ConnectionStrings__DefaultConnection");

            // Singleton Npgsql data source — used by TenantService and CORS origin check
            var npgsqlDataSource = NpgsqlDataSource.Create(connStr!);
            builder.Services.AddSingleton(npgsqlDataSource);

            // CORS — allows Next.js frontend and any configured origins
            var allowedOrigins = builder.Configuration
                .GetSection("AllowedOrigins").Get<string[]>()
                ?? [];
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("ApiCors", policy =>
                {
                    if (allowedOrigins.Length == 0)
                    {
                        policy.SetIsOriginAllowed(_ => true);
                    }
                    else
                    {
                        policy.SetIsOriginAllowed(origin =>
                        {
                            if (allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                                return true;

                            try
                            {
                                var host = new Uri(origin).Host.ToLower();
                                using var conn = npgsqlDataSource.OpenConnection();
                                using var cmd = conn.CreateCommand();
                                cmd.CommandText = """
                                    SELECT 1 FROM tenants
                                    WHERE LOWER("CustomDomain") = @host AND "IsActive" = true
                                    LIMIT 1
                                    """;
                                cmd.Parameters.AddWithValue("host", host);
                                return cmd.ExecuteScalar() is not null;
                            }
                            catch { return false; }
                        });
                    }
                    policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                });
            });

            // Database
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connStr));
            builder.Services.AddDbContextFactory<AppDbContext>(options =>
                options.UseNpgsql(connStr), ServiceLifetime.Scoped);

            // Identity
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            // Authorization policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("CanPreRegisterVisits",
                    p => p.RequireRole(RoleOccupant));
                options.AddPolicy("CanCheckInVisitors",
                    p => p.RequireRole(RoleSecurity, RoleManager, RoleAdmin));
                options.AddPolicy("CanManageVisitors",
                    p => p.RequireRole(RoleManager, RoleAdmin));
                options.AddPolicy("CanManageAccess",
                    p => p.RequireRole(RoleSecurity, RoleReceptionist, RoleManager, RoleAdmin));
                options.AddPolicy("CanViewReports",
                    p => p.RequireRole(RoleManager, RoleAdmin));
                options.AddPolicy("CanManageUsers",
                    p => p.RequireRole(RoleAdmin));
                options.AddPolicy("CanManageUnits",
                    p => p.RequireRole(RoleAdmin, RoleManager));
                options.AddPolicy("CanViewOwnHistory",
                    p => p.RequireAuthenticatedUser());
                options.AddPolicy("CanLogIncidents",
                    p => p.RequireRole(RoleSecurity, RoleReceptionist, RoleManager, RoleAdmin));
                options.AddPolicy("CanManageIncidents",
                    p => p.RequireRole(RoleManager, RoleAdmin));
                options.AddPolicy("CanAccessParking",
                    p => p.RequireRole(RoleSecurity, RoleManager, RoleAdmin));
                options.AddPolicy("CanManageParking",
                    p => p.RequireRole(RoleManager, RoleAdmin));
            });

            // Health checks
            builder.Services.AddHealthChecks()
                .AddDbContextCheck<AppDbContext>("database")
                .AddCheck("smtp", () =>
                {
                    var smtp = builder.Configuration.GetSection("Smtp").Get<SmtpSettings>() ?? new SmtpSettings();
                    return string.IsNullOrWhiteSpace(smtp.Host)
                        ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded("SMTP not configured.")
                        : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy();
                });

            // Rate limiting
            builder.Services.AddRateLimiter(rateLimiter =>
            {
                rateLimiter.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                {
                    var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "anon";
                    if (ctx.Request.Method == "POST" &&
                        (ctx.Request.Path.Value?.Contains("login", StringComparison.OrdinalIgnoreCase) == true ||
                         ctx.Request.Path.Value?.Contains("forgot-password", StringComparison.OrdinalIgnoreCase) == true))
                    {
                        return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter($"auth:{ip}", _ =>
                            new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 10,
                                Window = TimeSpan.FromMinutes(15),
                                QueueLimit = 0
                            });
                    }
                    return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
                        new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 500,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        });
                });
                rateLimiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            // DataProtection keys persisted to volume
            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new System.IO.DirectoryInfo("/app/keys"))
                .SetApplicationName("FacilityApp")
                .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

            // Trust Caddy's forwarded headers
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                         | ForwardedHeaders.XForwardedProto;
                options.KnownProxies.Clear();
                options.KnownIPNetworks.Clear();
            });

            // Multi-tenancy
            builder.Services.AddScoped<TenantContext>();
            builder.Services.AddScoped<ITenantService, TenantService>();

            // Controllers
            builder.Services.AddControllers();

            // JWT Authentication
            var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
            builder.Services.AddSingleton(jwtSettings);
            builder.Services.AddScoped<IJwtService, JwtService>();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = jwtSettings.Issuer,
                    ValidAudience            = jwtSettings.Audience,
                    IssuerSigningKey         = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.Secret.Length > 0
                            ? jwtSettings.Secret
                            : "placeholder-dev-secret-change-in-production")),
                    ClockSkew = TimeSpan.Zero
                };
            });

            // Email
            var smtp = builder.Configuration.GetSection("Smtp").Get<SmtpSettings>() ?? new SmtpSettings();
            builder.Services.AddSingleton(smtp);
            builder.Services.AddScoped<IEmailService, EmailService>();

            // SMS (Africa's Talking)
            var at = builder.Configuration.GetSection("AfricasTalking").Get<AfricasTalkingSettings>() ?? new AfricasTalkingSettings();
            builder.Services.AddSingleton(at);
            builder.Services.AddHttpClient("africastalking");
            builder.Services.AddScoped<ISmsService, SmsService>();

            // Application services
            builder.Services.AddScoped<IAuditService, AuditService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IUnitService, UnitService>();
            builder.Services.AddScoped<IVisitorService, VisitorService>();
            builder.Services.AddScoped<IAccessPassService, AccessPassService>();
            builder.Services.AddScoped<IBlacklistService, BlacklistService>();
            builder.Services.AddScoped<IFacilityService, FacilityService>();
            builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddScoped<ISettingsService, SettingsService>();
            builder.Services.AddScoped<IUnitRequestService, UnitRequestService>();
            builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();
            builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
            builder.Services.AddScoped<IDocumentService, DocumentService>();
            builder.Services.AddScoped<IIncidentService, IncidentService>();
            builder.Services.AddSingleton<IQrCodeService, QrCodeService>();
            builder.Services.AddScoped<IParkingService, ParkingService>();
            builder.Services.AddScoped<IParcelService, ParcelService>();
            builder.Services.AddScoped<IEntranceService, EntranceService>();

            var app = builder.Build();

            await MigrateAsync(app);
            await SeedRolesAsync(app);
            await SeedSuperAdminAsync(app);

            app.UseForwardedHeaders();

            if (!app.Environment.IsDevelopment())
                app.UseExceptionHandler("/error");

            // Security headers
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("Content-Security-Policy", "frame-ancestors 'none'");
                context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
                await next();
            });

            app.UseStaticFiles();
            app.UseRateLimiter();
            app.UseCors("ApiCors");
            app.UseMiddleware<FacilityApp.Middleware.TenantDomainMiddleware>();
            app.UseAuthentication();

            // Resolve TenantContext from JWT claims for API requests
            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api") &&
                    ctx.User.Identity?.IsAuthenticated == true)
                {
                    var tenantCtx = ctx.RequestServices.GetRequiredService<TenantContext>();
                    if (!tenantCtx.IsResolved)
                    {
                        var tenantIdClaim   = ctx.User.FindFirstValue("tenant_id");
                        var tenantSlugClaim = ctx.User.FindFirstValue("tenant_slug");
                        var tenantNameClaim = ctx.User.FindFirstValue("tenant_name");
                        if (Guid.TryParse(tenantIdClaim, out var tenantId))
                            tenantCtx.SetFromJwt(tenantId, tenantSlugClaim ?? "", tenantNameClaim ?? "");
                    }
                }
                await next();
            });

            app.UseAuthorization();
            app.MapControllers();

            // Dev-only seed endpoints
            if (app.Environment.IsDevelopment())
            {
                app.MapGet("/dev/seed-admin", async (
                    AppDbContext db,
                    UserManager<ApplicationUser> userManager,
                    string? tenantName,
                    string? tenantSlug,
                    string? email,
                    string? password,
                    string? fullName) =>
                {
                    tenantName ??= "Default Facility";
                    tenantSlug ??= "default";
                    email      ??= "admin@facility.com";
                    password   ??= "Admin1234";
                    fullName   ??= "System Admin";

                    var tenant = db.Tenants.FirstOrDefault(t => t.Slug == tenantSlug);
                    if (tenant is null)
                    {
                        tenant = new Tenant { Id = Guid.NewGuid(), Name = tenantName, Slug = tenantSlug, IsActive = true, CreatedAt = DateTime.UtcNow };
                        db.Tenants.Add(tenant);
                        await db.SaveChangesAsync();
                    }

                    var existing = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email);
                    if (existing is not null)
                        return Results.Ok($"User {email} already exists.");

                    var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, TenantId = tenant.Id, UserType = UserType.Staff };
                    var result = await userManager.CreateAsync(user, password);
                    if (!result.Succeeded)
                        return Results.BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));

                    await userManager.AddToRoleAsync(user, RoleAdmin);
                    return Results.Ok(new { Message = "Admin seeded.", TenantSlug = tenant.Slug, Email = email, Password = password });
                });

                app.MapGet("/dev/seed-superadmin", async (
                    AppDbContext db,
                    UserManager<ApplicationUser> userManager,
                    string? email,
                    string? password,
                    string? fullName) =>
                {
                    email    ??= "superadmin@platform.local";
                    password ??= "SuperAdmin1234";
                    fullName ??= "Platform SuperAdmin";

                    var tenant = db.Tenants.IgnoreQueryFilters().FirstOrDefault(t => t.Slug == "platform");
                    if (tenant is null)
                    {
                        tenant = new Tenant { Id = Guid.NewGuid(), Name = "Platform", Slug = "platform", IsSystem = true, IsActive = true, Plan = TenantPlan.Professional, CreatedAt = DateTime.UtcNow };
                        db.Tenants.Add(tenant);
                        await db.SaveChangesAsync();
                    }

                    var existing = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email);
                    if (existing is not null)
                        return Results.Ok($"User {email} already exists.");

                    var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, TenantId = tenant.Id, UserType = UserType.Staff };
                    var result = await userManager.CreateAsync(user, password);
                    if (!result.Succeeded)
                        return Results.BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));

                    await userManager.AddToRoleAsync(user, RoleSuperAdmin);
                    return Results.Ok(new { Message = "SuperAdmin seeded.", Email = email, Password = password });
                });

                app.MapGet("/dev/reset-password", async (string email, string newPassword, AppDbContext db, UserManager<ApplicationUser> userManager) =>
                {
                    var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email);
                    if (user is null) return Results.NotFound($"No user found: {email}");
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    var result = await userManager.ResetPasswordAsync(user, token, newPassword);
                    return result.Succeeded ? Results.Ok($"Password reset for {email}.") : Results.BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));
                });
            }

            app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                ResponseWriter = async (ctx, report) =>
                {
                    ctx.Response.ContentType = "application/json";
                    var result = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString(), description = e.Value.Description })
                    });
                    await ctx.Response.WriteAsync(result);
                }
            });

            app.Run();
        }

        private static async Task MigrateAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
        }

        private static async Task SeedRolesAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            string[] roles = [RoleAdmin, RoleManager, RoleReceptionist, RoleSecurity, RoleOccupant, RoleSuperAdmin];
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        private static async Task SeedSuperAdminAsync(WebApplication app)
        {
            var email    = Environment.GetEnvironmentVariable("SUPERADMIN_EMAIL");
            var password = Environment.GetEnvironmentVariable("SUPERADMIN_PASSWORD");
            var fullName = Environment.GetEnvironmentVariable("SUPERADMIN_FULLNAME") ?? "Platform SuperAdmin";
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

            using var scope = app.Services.CreateScope();
            var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var tenant = db.Tenants.IgnoreQueryFilters().FirstOrDefault(t => t.Slug == "platform");
            if (tenant is null)
            {
                tenant = new Tenant
                {
                    Id        = Guid.NewGuid(),
                    Name      = "Platform",
                    Slug      = "platform",
                    IsSystem  = true,
                    IsActive  = true,
                    Plan      = TenantPlan.Professional,
                    CreatedAt = DateTime.UtcNow,
                };
                db.Tenants.Add(tenant);
                await db.SaveChangesAsync();
            }

            var existing = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email);
            if (existing is not null) return;

            var user = new ApplicationUser
            {
                UserName = email,
                Email    = email,
                FullName = fullName,
                TenantId = tenant.Id,
                UserType = UserType.Staff,
            };
            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(user, RoleSuperAdmin);
        }
    }
}
