# Build stage — produces the publish output
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy solution files first for layer caching
COPY FlowHub.slnx Directory.Build.props Directory.Packages.props global.json .editorconfig ./
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

USER appuser

EXPOSE 5070
ENV ASPNETCORE_URLS=http://+:5070

ENTRYPOINT ["dotnet", "FlowHub.Web.dll"]
