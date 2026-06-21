# Build stage — produces the publish output
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy solution files first for layer caching
COPY FlowHub.slnx Directory.Build.props Directory.Packages.props CodeMetricsConfig.txt global.json .editorconfig ./
COPY source/ ./source/
COPY tests/ ./tests/

RUN dotnet restore FlowHub.slnx

RUN dotnet publish source/FlowHub.Web/FlowHub.Web.csproj \
    --no-restore \
    --configuration Release \
    --output /app/publish \
    --self-contained false

# Runtime stage — minimal Alpine image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

RUN addgroup -S appgroup && adduser -S appuser -G appgroup

COPY --from=build /app/publish ./

# Attachment uploads are written under the content root (App_Data/uploads — see
# UploadOptions.StoragePath). Create it writable by the non-root runtime user;
# without this, file captures (REST POST /api/v1/captures/upload and the Blazor
# /captures/new page) fail with UnauthorizedAccessException. Ephemeral by design.
RUN mkdir -p /app/App_Data/uploads && chown -R appuser:appgroup /app/App_Data

USER appuser

EXPOSE 5070
ENV ASPNETCORE_URLS=http://+:5070

ENTRYPOINT ["dotnet", "FlowHub.Web.dll"]
