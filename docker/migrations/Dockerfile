# Build the EF Core migrations bundle — self-contained native binary
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY FlowHub.slnx Directory.Build.props Directory.Packages.props CodeMetricsConfig.txt global.json .editorconfig ./
COPY source/ ./source/
COPY tests/ ./tests/

RUN dotnet restore FlowHub.slnx

RUN dotnet tool install --global dotnet-ef && \
    export PATH="$PATH:/root/.dotnet/tools" && \
    dotnet ef migrations bundle \
        --project source/FlowHub.Persistence \
        --startup-project source/FlowHub.Web \
        --output /efbundle \
        --self-contained \
        --configuration Release

# Runtime stage — just the bundle binary + Alpine runtime libs
FROM alpine:3.20 AS runtime
RUN apk add --no-cache libstdc++ libgcc icu-libs
COPY --from=build /efbundle /efbundle
ENTRYPOINT ["/efbundle"]
