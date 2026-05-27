# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0.203 AS build
WORKDIR /src

COPY FacilityApp.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# Ensure /_framework/blazor.web.js is available at the plain URL.
# .NET 10 SDK behaviour varies by version:
#   - some builds: file is fingerprinted (blazor.web.<hash>.js) — copy to plain name
#   - some builds: file is already published as blazor.web.js — nothing to do
#   - newer SDK builds (10.0.203+): file is served from embedded runtime resources
#     and is NOT present in the publish output at all — rely on MapStaticAssets()
# Non-fatal: if no file is found the runtime fallback in Program.cs handles it.
RUN mkdir -p /app/publish/wwwroot/_framework && \
    plain=/app/publish/wwwroot/_framework/blazor.web.js && \
    if [ -f "$plain" ]; then \
        echo "blazor.web.js already present (plain publish)."; \
    else \
        fp=$(find /app/publish -path "*/_framework/blazor.web*.js" ! -name "*.map" | head -1); \
        if [ -n "$fp" ]; then \
            echo "Found blazor.web.js variant: $fp"; \
            cp "$fp" "$plain"; \
            echo "Plain copy created."; \
        else \
            echo "WARNING: blazor.web.js not found in publish output."; \
            echo "  _framework contents:"; \
            ls -la /app/publish/wwwroot/_framework/ 2>/dev/null || echo "  (directory empty or missing)"; \
            echo "  Runtime fallback in Program.cs will serve the file from embedded resources."; \
        fi; \
    fi

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0.7 AS runtime
WORKDIR /app

# Install missing native libs required by Npgsql (GSSAPI/Kerberos)
RUN apt-get update \
 && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
 && rm -rf /var/lib/apt/lists/*

# Create non-root user (Debian-based image)
RUN groupadd --system --gid 1001 appgroup \
 && useradd  --system --uid 1001 --gid appgroup appuser

# Copy published output
COPY --from=build /app/publish .

# Writable directories for uploads/documents/dataprotection keys
RUN mkdir -p wwwroot/uploads wwwroot/documents keys \
 && chown -R appuser:appgroup wwwroot/uploads wwwroot/documents keys

USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "FacilityApp.dll"]
