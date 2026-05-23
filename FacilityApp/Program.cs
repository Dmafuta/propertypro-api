using FacilityApp.Components;
using FacilityApp.Data;
using FacilityApp.Data.Models;
using FacilityApp.Hubs;
using FacilityApp.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

            // Fail fast if connection string is missing (set via ConnectionStrings__DefaultConnection env var in production)
            var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connStr))
                throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' is not configured. " +
                    "Set the environment variable: ConnectionStrings__DefaultConnection");

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents(options =>
                {
                    // Keep disconnected circuits for 3 minutes, retain max 100 to cap memory use
                    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
                    options.DisconnectedCircuitMaxRetained = 100;
                });

            builder.Services.AddSignalR();

            // CORS for the Blazor SignalR circuit (/_blazor) and notification hub.
            // When an explicit CORS policy with AllowCredentials() is present,
            // ASP.NET Core SignalR defers origin validation to CORS instead of
            // doing its own same-origin check — this is the recommended approach
            // for production deployments behind a reverse proxy.
            var allowedOrigins = builder.Configuration
                .GetSection("AllowedOrigins").Get<string[]>()
                ?? [];
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("BlazorHub", policy =>
                {
                    if (allowedOrigins.Length == 0)
                    {
                        // Dev / unconfigured: allow all origins
                        policy.SetIsOriginAllowed(_ => true);
                    }
                    else
                    {
                        // Production: allow explicitly listed origins, plus any active
                        // tenant custom domain resolved from the database at request time.
                        // Adding a new custom-domain tenant works immediately without
                        // restarting the app or updating the AllowedOrigins list.
                        policy.SetIsOriginAllowed(origin =>
                        {
                            if (allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                                return true;

                            try
                            {
                                var host = new Uri(origin).Host.ToLower();
                                using var conn = new NpgsqlConnection(connStr);
                                conn.Open();
                                using var cmd = conn.CreateCommand();
                                cmd.CommandText = """
                                    SELECT 1 FROM tenants
                                    WHERE LOWER("CustomDomain") = @host AND "IsActive" = true
                                    LIMIT 1
                                    """;
                                cmd.Parameters.AddWithValue("host", host);
                                return cmd.ExecuteScalar() is not null;
                            }
                            catch
                            {
                                return false;
                            }
                        });
                    }
                    policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                });
            });

            // Singleton Npgsql data source — used by TenantService for concurrent-safe tenant resolution
            builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connStr!));

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

            // Cookie auth — redirect to tenant-aware login path
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);

                options.Events.OnRedirectToLogin = ctx =>
                {
                    var tenantCtx  = ctx.HttpContext.RequestServices.GetRequiredService<TenantContext>();
                    var returnUrl  = Uri.EscapeDataString(ctx.Request.Path);
                    var segments   = ctx.Request.Path.ToString().Trim('/').Split('/');

                    string loginPath;
                    if (tenantCtx.IsCustomDomain)
                    {
                        var isResident = segments.Length > 0 &&
                                         segments[0].Equals("resident", StringComparison.OrdinalIgnoreCase);
                        loginPath = isResident
                            ? $"/resident/login?returnUrl={returnUrl}"
                            : $"/login?returnUrl={returnUrl}";
                    }
                    else
                    {
                        var slug       = segments.FirstOrDefault() ?? string.Empty;
                        var isResident = segments.Length > 1 &&
                                         segments[1].Equals("resident", StringComparison.OrdinalIgnoreCase);
                        loginPath = isResident
                            ? $"/{slug}/resident/login?returnUrl={returnUrl}"
                            : $"/{slug}/login?returnUrl={returnUrl}";
                    }

                    ctx.Response.Redirect(loginPath);
                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToAccessDenied = ctx =>
                {
                    var tenantCtx  = ctx.HttpContext.RequestServices.GetRequiredService<TenantContext>();
                    var segments   = ctx.Request.Path.ToString().Trim('/').Split('/');

                    string deniedPath;
                    if (tenantCtx.IsCustomDomain)
                    {
                        var isResident = segments.Length > 0 &&
                                         segments[0].Equals("resident", StringComparison.OrdinalIgnoreCase);
                        deniedPath = isResident ? "/resident/login" : "/access-denied";
                    }
                    else
                    {
                        var slug       = segments.FirstOrDefault() ?? string.Empty;
                        var isResident = segments.Length > 1 &&
                                         segments[1].Equals("resident", StringComparison.OrdinalIgnoreCase);
                        deniedPath = isResident
                            ? $"/{slug}/resident/login"
                            : $"/{slug}/access-denied";
                    }

                    ctx.Response.Redirect(deniedPath);
                    return Task.CompletedTask;
                };
            });

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
                        ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded("SMTP not configured — password reset emails will not be delivered.")
                        : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy();
                });

            // Rate limiting — per-IP, protects login and general DoS
            builder.Services.AddRateLimiter(rateLimiter =>
            {
                rateLimiter.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                {
                    var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "anon";

                    // Strict limit on login/forgot-password POST submissions
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

                    // General per-IP limiter — prevents DoS
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

            // Persist DataProtection keys to a volume so sessions survive container restarts
            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new System.IO.DirectoryInfo("/app/keys"))
                .SetApplicationName("FacilityApp")
                .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

            // Trust Caddy's X-Forwarded-For (real client IP) and X-Forwarded-Proto (https).
            // XForwardedHost is NOT needed — Caddy is configured with header_up Host {host}
            // so the app receives the real hostname directly in the Host header.
            // KnownProxies/Networks are cleared so Caddy's Docker-internal IP is trusted.
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
            builder.Services.AddScoped<GateContext>();
            builder.Services.AddScoped<IEntranceService, EntranceService>();

            // Email
            var smtp = builder.Configuration.GetSection("Smtp").Get<SmtpSettings>() ?? new SmtpSettings();
            builder.Services.AddSingleton(smtp);
            builder.Services.AddScoped<IEmailService, EmailService>();

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

            var app = builder.Build();

            // Run pending EF Core migrations on startup
            await MigrateAsync(app);

            // Seed Identity roles on startup
            await SeedRolesAsync(app);

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            // Exclude Blazor framework paths from status-code re-execution so a 404 on
            // /_framework/blazor.web.js is never converted to an HTML page (which causes
            // "Refused to execute script" MIME-type errors in the browser).
            app.UseWhen(
                ctx => !ctx.Request.Path.StartsWithSegments("/_framework") &&
                       !ctx.Request.Path.StartsWithSegments("/_blazor"),
                b => b.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));

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

            // Blazor.web.js fallback — must run BEFORE UseStaticFiles so that even if
            // the static file middleware fails to open the file (permissions, lock, etc.)
            // the framework JS is always served with the correct MIME type.
            // Handles both the plain URL (_framework/blazor.web.js) AND any fingerprinted
            // variant (_framework/blazor.web.{hash}.js) that @Assets may generate.
            {
                string? blazorJsPath = null;
                foreach (var searchRoot in new[]
                {
                    Path.Combine(app.Environment.WebRootPath ?? "", "_framework"),
                    Path.Combine(app.Environment.ContentRootPath, "wwwroot", "_framework"),
                    Path.Combine(app.Environment.ContentRootPath, "_framework"),
                })
                {
                    if (!Directory.Exists(searchRoot)) continue;

                    // Prefer the plain file (SDK 10.0.203 publishes it as plain)
                    var plain = Path.Combine(searchRoot, "blazor.web.js");
                    if (File.Exists(plain)) { blazorJsPath = plain; break; }

                    // Fall back to any fingerprinted variant
                    var fingerprinted = Directory
                        .GetFiles(searchRoot, "blazor.web*.js")
                        .Where(f => !f.EndsWith(".map", StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault();
                    if (fingerprinted != null) { blazorJsPath = fingerprinted; break; }
                }

                if (blazorJsPath != null)
                {
                    var cachedBytes = File.ReadAllBytes(blazorJsPath);
                    app.Use(async (ctx, next) =>
                    {
                        var path = ctx.Request.Path.Value ?? "";
                        // Match plain URL and any fingerprinted variant
                        if (path.StartsWith("/_framework/blazor.web", StringComparison.OrdinalIgnoreCase)
                            && path.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                        {
                            ctx.Response.ContentType = "application/javascript";
                            ctx.Response.Headers.CacheControl = "no-cache";
                            await ctx.Response.Body.WriteAsync(cachedBytes);
                            return;
                        }
                        await next(ctx);
                    });
                }
            }

            app.UseStaticFiles(); // serves wwwroot files
            app.UseRateLimiter();
            app.UseCors("BlazorHub"); // must be before UseAuthentication for SignalR WebSocket
            app.UseMiddleware<FacilityApp.Middleware.TenantDomainMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAntiforgery();

            // Staff logout
            app.MapPost("/{tenantSlug}/api/logout", async (
                string tenantSlug,
                SignInManager<ApplicationUser> signInManager) =>
            {
                await signInManager.SignOutAsync();
                return Results.Redirect($"/{tenantSlug}/login");
            });

            // Resident logout
            app.MapPost("/{tenantSlug}/api/resident/logout", async (
                string tenantSlug,
                SignInManager<ApplicationUser> signInManager) =>
            {
                await signInManager.SignOutAsync();
                return Results.Redirect($"/{tenantSlug}/resident/login");
            });

            // Slug-free logout endpoints for custom-domain deployments
            app.MapPost("/api/logout", async (SignInManager<ApplicationUser> signInManager) =>
            {
                await signInManager.SignOutAsync();
                return Results.Redirect("/login");
            });

            app.MapPost("/api/resident/logout", async (SignInManager<ApplicationUser> signInManager) =>
            {
                await signInManager.SignOutAsync();
                return Results.Redirect("/resident/login");
            });

            // Slug-free CSV export for custom-domain deployments
            app.MapGet("/api/reports/export", async (
                string? from,
                string? to,
                IReportService reportSvc,
                TenantContext tenantCtx,
                HttpContext httpCtx) =>
            {
                if (!(httpCtx.User.Identity?.IsAuthenticated ?? false))
                    return Results.Unauthorized();
                if (!httpCtx.User.IsInRole(RoleManager) && !httpCtx.User.IsInRole(RoleAdmin))
                    return Results.Forbid();
                if (tenantCtx.TenantId == Guid.Empty)
                    return Results.NotFound();

                var fromDate = DateOnly.TryParse(from, out var fd)
                    ? fd : DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
                var toDate = DateOnly.TryParse(to, out var td)
                    ? td : DateOnly.FromDateTime(DateTime.Today);

                var bytes    = await reportSvc.GetCsvBytesAsync(fromDate, toDate);
                var filename = $"visits-{fromDate:yyyy-MM-dd}-to-{toDate:yyyy-MM-dd}.csv";
                return Results.File(bytes, "text/csv; charset=utf-8", filename);
            });

            // CSV export endpoint
            app.MapGet("/{tenantSlug}/api/reports/export", async (
                string tenantSlug,
                string? from,
                string? to,
                IReportService reportSvc,
                ITenantService tenantSvc,
                TenantContext tenantCtx,
                HttpContext httpCtx) =>
            {
                if (!(httpCtx.User.Identity?.IsAuthenticated ?? false))
                    return Results.Unauthorized();

                if (!httpCtx.User.IsInRole(RoleManager) && !httpCtx.User.IsInRole(RoleAdmin))
                    return Results.Forbid();

                var tenant = await tenantSvc.ResolveBySlugAsync(tenantSlug);
                if (tenant is null) return Results.NotFound();

                // Verify the caller belongs to this tenant (SuperAdmin may export any tenant)
                if (!httpCtx.User.IsInRole(RoleSuperAdmin))
                {
                    var userIdClaim = httpCtx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (userIdClaim is null) return Results.Unauthorized();
                    var userMgr = httpCtx.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                    var appUser = await userMgr.FindByIdAsync(userIdClaim);
                    if (appUser is null || appUser.TenantId != tenant.Id)
                        return Results.Forbid();
                }

                tenantCtx.TenantId = tenant.Id;

                var fromDate = DateOnly.TryParse(from, out var fd)
                    ? fd : DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
                var toDate = DateOnly.TryParse(to, out var td)
                    ? td : DateOnly.FromDateTime(DateTime.Today);

                var bytes    = await reportSvc.GetCsvBytesAsync(fromDate, toDate);
                var filename = $"visits-{fromDate:yyyy-MM-dd}-to-{toDate:yyyy-MM-dd}.csv";
                return Results.File(bytes, "text/csv; charset=utf-8", filename);
            });

            // ⚠️  TEMPORARY — remove after use
            if (app.Environment.IsDevelopment())
            {
                app.MapGet("/dev/list-users", (AppDbContext db) =>
                {
                    var users = db.Users.IgnoreQueryFilters()
                        .Select(u => new { u.Email, u.FullName, u.UserType, u.TenantId })
                        .ToList();
                    return Results.Ok(users);
                });

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

                    // Create tenant if it doesn't exist
                    var tenant = db.Tenants.FirstOrDefault(t => t.Slug == tenantSlug);
                    if (tenant is null)
                    {
                        tenant = new Tenant
                        {
                            Id        = Guid.NewGuid(),
                            Name      = tenantName,
                            Slug      = tenantSlug,
                            IsActive  = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        db.Tenants.Add(tenant);
                        await db.SaveChangesAsync();
                    }

                    // Create admin user if not already present
                    var existing = await db.Users.IgnoreQueryFilters()
                                          .FirstOrDefaultAsync(u => u.Email == email);
                    if (existing is not null)
                        return Results.Ok($"User {email} already exists. Use /dev/reset-password to change the password.");

                    var user = new ApplicationUser
                    {
                        UserName  = email,
                        Email     = email,
                        FullName  = fullName,
                        TenantId  = tenant.Id,
                        UserType  = UserType.Staff
                    };

                    var result = await userManager.CreateAsync(user, password);
                    if (!result.Succeeded)
                        return Results.BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));

                    await userManager.AddToRoleAsync(user, RoleAdmin);

                    return Results.Ok(new
                    {
                        Message    = "Admin seeded successfully.",
                        TenantSlug = tenant.Slug,
                        Email      = email,
                        Password   = password,
                        LoginUrl   = $"/{tenant.Slug}/login"
                    });
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

                    // Create the system tenant if it doesn't exist
                    var tenant = db.Tenants.IgnoreQueryFilters()
                                   .FirstOrDefault(t => t.Slug == "platform");
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
                            CreatedAt = DateTime.UtcNow
                        };
                        db.Tenants.Add(tenant);
                        await db.SaveChangesAsync();
                    }

                    // Create the superadmin user if not present
                    var existing = await db.Users.IgnoreQueryFilters()
                                           .FirstOrDefaultAsync(u => u.Email == email);
                    if (existing is not null)
                        return Results.Ok($"User {email} already exists.");

                    var user = new ApplicationUser
                    {
                        UserName = email,
                        Email    = email,
                        FullName = fullName,
                        TenantId = tenant.Id,
                        UserType = UserType.Staff
                    };

                    var result = await userManager.CreateAsync(user, password);
                    if (!result.Succeeded)
                        return Results.BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));

                    await userManager.AddToRoleAsync(user, RoleSuperAdmin);

                    return Results.Ok(new
                    {
                        Message   = "SuperAdmin seeded successfully.",
                        LoginUrl  = "/platform/login",
                        Email     = email,
                        Password  = password
                    });
                });

                app.MapGet("/dev/grant-superadmin", async (
                    string email,
                    AppDbContext db,
                    UserManager<ApplicationUser> userManager) =>
                {
                    var user = await db.Users.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(u => u.Email == email);
                    if (user is null) return Results.NotFound($"No user found: {email}");
                    if (await userManager.IsInRoleAsync(user, RoleSuperAdmin))
                        return Results.Ok($"{email} already has SuperAdmin.");
                    var result = await userManager.AddToRoleAsync(user, RoleSuperAdmin);
                    return result.Succeeded
                        ? Results.Ok($"SuperAdmin granted to {email}.")
                        : Results.BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));
                });

                app.MapGet("/dev/reset-password", async (
                    string email,
                    string newPassword,
                    AppDbContext db,
                    UserManager<ApplicationUser> userManager) =>
                {
                    var user = await db.Users.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(u => u.Email == email);
                    if (user is null)
                        return Results.NotFound($"No user found with email: {email}");

                    var token  = await userManager.GeneratePasswordResetTokenAsync(user);
                    var result = await userManager.ResetPasswordAsync(user, token, newPassword);

                    return result.Succeeded
                        ? Results.Ok($"Password for {email} has been reset successfully.")
                        : Results.BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));
                });
            }

            app.MapHub<NotificationHub>("/hubs/notifications");
            app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                ResponseWriter = async (ctx, report) =>
                {
                    ctx.Response.ContentType = "application/json";
                    var result = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        status  = report.Status.ToString(),
                        checks  = report.Entries.Select(e => new
                        {
                            name        = e.Key,
                            status      = e.Value.Status.ToString(),
                            description = e.Value.Description
                        })
                    });
                    await ctx.Response.WriteAsync(result);
                }
            });

            // MapStaticAssets must come before MapRazorComponents so the static web
            // assets manifest (which resolves @Assets fingerprinted paths) is registered
            // before any Razor page renders.
            app.MapStaticAssets();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

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
    }
}
