# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FacilityApp.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# Ensure /_framework/blazor.web.js is available at the plain URL.
# .NET 10 SDK behaviour varies by version:
#   - older builds: file is fingerprinted (blazor.web.<hash>.js) — copy to plain name
#   - newer builds: file is already published as blazor.web.js — nothing to do
# Fails the build if neither form is found so CI catches broken publish layouts.
RUN set -e && \
    mkdir -p /app/publish/wwwroot/_framework && \
    plain=/app/publish/wwwroot/_framework/blazor.web.js && \
    if [ -f "$plain" ]; then \
        echo "blazor.web.js already present (plain publish)."; \
    else \
        fp=$(find /app/publish -path "*/_framework/blazor.web.*.js" ! -name "*.map" | head -1); \
        if [ -n "$fp" ]; then \
            echo "Found fingerprinted blazor.web.js: $fp"; \
            cp "$fp" "$plain"; \
            echo "Plain copy created."; \
        else \
            echo "ERROR: blazor.web.js not found in any expected location."; \
            find /app/publish -name "_framework" -type d || true; \
            find /app/publish -name "blazor*" 2>/dev/null || true; \
            exit 1; \
        fi; \
    fi

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

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
