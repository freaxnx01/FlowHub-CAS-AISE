# Block 4 — Slice 1: Foundation (May 5–10)

**Rubric targets:** Use Cases (5), NfA SMART (5), Solution Vision (5), Code strukturiert (partial 7), Source in Git (2)

**Prerequisites:** Beta MVP landed. PostgreSQL available via Docker or locally.

---

### Task 1: PostgreSQL Switch

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `source/FlowHub.Persistence/FlowHub.Persistence.csproj`
- Modify: `source/FlowHub.Persistence/PersistenceServiceCollectionExtensions.cs`
- Modify: `source/FlowHub.Persistence/FlowHubDbContextFactory.cs`
- Modify: `docker-compose.yml`
- Modify: `Makefile`
- Delete: `source/FlowHub.Persistence/Migrations/20260504120638_Initial.cs` + `.Designer.cs`
- Regenerate: `source/FlowHub.Persistence/Migrations/` (new 0001_Initial for PostgreSQL)

- [ ] **Step 1: Add Npgsql package to central package management**

In `Directory.Packages.props`, replace the `Label="Persistence"` group:

```xml
<ItemGroup Label="Persistence">
  <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.7" />
  <PackageVersion Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.7" />
  <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.7" />
  <PackageVersion Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.7" />
  <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.4" />
  <PackageVersion Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.7" />
  <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.7" />
</ItemGroup>
```

> If `dotnet build` later reports an EF Core 10 compatibility error for Npgsql 9.0.4, run `dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --project source/FlowHub.Persistence/FlowHub.Persistence.csproj` without `--version` to discover the latest compatible release, note the version, then set it in `Directory.Packages.props` and remove the `<Version>` from the .csproj.

- [ ] **Step 2: Swap SQLite for Npgsql in the persistence project**

Replace `source/FlowHub.Persistence/FlowHub.Persistence.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>FlowHub.Persistence</RootNamespace>
    <AssemblyName>FlowHub.Persistence</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    <PackageReference Include="MassTransit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\FlowHub.Core\FlowHub.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Switch PersistenceServiceCollectionExtensions to UseNpgsql**

Replace `source/FlowHub.Persistence/PersistenceServiceCollectionExtensions.cs`:

```csharp
using FlowHub.Core.Captures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlowHub.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=flowhub;Username=flowhub;Password=dev-secret";

    public static IServiceCollection AddFlowHubPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? DefaultConnectionString;

        services.AddDbContext<FlowHubDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ICaptureService, EfCaptureService>();
        services.AddHostedService<MigrationRunner>();

        return services;
    }
}

internal sealed partial class MigrationRunner : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MigrationRunner> _log;

    public MigrationRunner(IServiceProvider services, ILogger<MigrationRunner> log)
    {
        _services = services;
        _log = log;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowHubDbContext>();
        LogApplyingMigrations();
        await db.Database.MigrateAsync(cancellationToken);
        LogMigrationsApplied();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(EventId = 5010, Level = LogLevel.Information, Message = "Applying EF Core migrations…")]
    private partial void LogApplyingMigrations();

    [LoggerMessage(EventId = 5011, Level = LogLevel.Information, Message = "EF Core migrations up-to-date.")]
    private partial void LogMigrationsApplied();
}
```

- [ ] **Step 4: Update FlowHubDbContextFactory for Npgsql**

Replace `source/FlowHub.Persistence/FlowHubDbContextFactory.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FlowHub.Persistence;

public sealed class FlowHubDbContextFactory : IDesignTimeDbContextFactory<FlowHubDbContext>
{
    public FlowHubDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=flowhub;Username=flowhub;Password=dev-secret";

        var options = new DbContextOptionsBuilder<FlowHubDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new FlowHubDbContext(options);
    }
}
```

- [ ] **Step 5: Add postgres service to docker-compose.yml**

In `docker-compose.yml`, add a `postgres` service and update the `flowhub.web` service to depend on it. The existing file has `rabbitmq` and `flowhub.web` already. Add before `flowhub.web`:

```yaml
  postgres:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: flowhub
      POSTGRES_USER: flowhub
      POSTGRES_PASSWORD: dev-secret
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U flowhub -d flowhub"]
      interval: 5s
      timeout: 5s
      retries: 5
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

Also add `depends_on: {postgres: {condition: service_healthy}}` to the `flowhub.web` service.

- [ ] **Step 6: Add db-up and db-migrate targets to Makefile**

In `Makefile`, add after the `format` target (keep `.PHONY` list updated):

```makefile
db-up: ## Start PostgreSQL in Docker (detached, waits until healthy)
	docker compose up postgres -d --wait

db-migrate: ## Apply EF Core migrations against the Docker PostgreSQL
	ConnectionStrings__Default="Host=localhost;Port=5432;Database=flowhub;Username=flowhub;Password=dev-secret" \
		dotnet ef database update \
		--project source/FlowHub.Persistence \
		--startup-project source/FlowHub.Web
```

Add `db-up db-migrate` to the `.PHONY` line.

- [ ] **Step 7: Drop old SQLite migrations and regenerate for PostgreSQL**

```bash
# Ensure postgres is running
make db-up

# Export connection string for design-time tools
export ConnectionStrings__Default="Host=localhost;Port=5432;Database=flowhub;Username=flowhub;Password=dev-secret"

# Remove the SQLite migration
dotnet ef migrations remove \
  --project source/FlowHub.Persistence \
  --startup-project source/FlowHub.Web

# Verify Migrations/ folder is empty (no .cs files remain)
ls source/FlowHub.Persistence/Migrations/

# Add new PostgreSQL-compatible initial migration
dotnet ef migrations add 0001_Initial \
  --project source/FlowHub.Persistence \
  --startup-project source/FlowHub.Web
```

Expected: A new `Migrations/YYYYMMDDHHMMSS_0001_Initial.cs` is created. Check it for `CREATE TABLE "Captures"` (quoted identifiers = PostgreSQL style).

- [ ] **Step 8: Verify build and existing tests pass**

```bash
dotnet build FlowHub.slnx
```
Expected: 0 errors, 0 warnings.

```bash
dotnet test FlowHub.slnx --filter "Category!=AI&Category!=BetaSmoke"
```
Expected: All previously passing tests still pass. `EfCaptureServiceTests` will fail (constructor changed in Task 3) — that's expected; note it and proceed.

- [ ] **Step 9: Commit**

```bash
git add Directory.Packages.props \
  source/FlowHub.Persistence/FlowHub.Persistence.csproj \
  source/FlowHub.Persistence/PersistenceServiceCollectionExtensions.cs \
  source/FlowHub.Persistence/FlowHubDbContextFactory.cs \
  source/FlowHub.Persistence/Migrations/ \
  docker-compose.yml \
  Makefile
git commit -m "feat(persistence): switch provider from SQLite to PostgreSQL"
```

---

### Task 2: CaptureEntityTypeConfiguration

**Files:**
- Create: `source/FlowHub.Persistence/Entities/CaptureEntityTypeConfiguration.cs`
- Modify: `source/FlowHub.Persistence/FlowHubDbContext.cs`

- [ ] **Step 1: Create CaptureEntityTypeConfiguration**

```csharp
// source/FlowHub.Persistence/Entities/CaptureEntityTypeConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowHub.Persistence.Entities;

internal sealed class CaptureEntityTypeConfiguration : IEntityTypeConfiguration<CaptureEntity>
{
    public void Configure(EntityTypeBuilder<CaptureEntity> builder)
    {
        builder.ToTable("Captures");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content).IsRequired();
        builder.Property(c => c.Source).IsRequired().HasMaxLength(32);
        builder.Property(c => c.Stage).IsRequired().HasMaxLength(32);
        builder.Property(c => c.MatchedSkill).HasMaxLength(64);
        builder.Property(c => c.Title).HasMaxLength(512);
        builder.Property(c => c.ExternalRef).HasMaxLength(256);

        builder.HasIndex(c => c.Stage).HasDatabaseName("IX_Captures_Stage");
        builder.HasIndex(c => c.CreatedAt).IsDescending().HasDatabaseName("IX_Captures_CreatedAt_DESC");
        builder.HasIndex(c => c.MatchedSkill).HasDatabaseName("IX_Captures_MatchedSkill");
    }
}
```

- [ ] **Step 2: Replace inline OnModelCreating with ApplyConfigurationsFromAssembly**

Replace `FlowHubDbContext.OnModelCreating`:

```csharp
// source/FlowHub.Persistence/FlowHubDbContext.cs
using FlowHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowHub.Persistence;

public sealed class FlowHubDbContext : DbContext
{
    public FlowHubDbContext(DbContextOptions<FlowHubDbContext> options) : base(options) { }

    internal DbSet<CaptureEntity> Captures => Set<CaptureEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlowHubDbContext).Assembly);
    }
}
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build FlowHub.slnx
```
Expected: 0 errors. The new `IX_Captures_MatchedSkill` index will be picked up in the next migration (created in Task 3's DI registration step, but no new migration needed yet — add to 0001_Initial by regenerating if index was missing from it).

> Note: If the 0001_Initial migration (from Task 1) already ran against the DB and is missing the new index, add the index via a migration:
> ```bash
> dotnet ef migrations add 0001_AddCaptureMatchedSkillIndex \
>   --project source/FlowHub.Persistence \
>   --startup-project source/FlowHub.Web
> ```
> Since no production data exists yet, it's cleaner to delete all migrations and re-run `dotnet ef migrations add 0001_Initial` once to capture the full initial schema including this index.

- [ ] **Step 4: Commit**

```bash
git add source/FlowHub.Persistence/Entities/CaptureEntityTypeConfiguration.cs \
  source/FlowHub.Persistence/FlowHubDbContext.cs \
  source/FlowHub.Persistence/Migrations/
git commit -m "refactor(persistence): extract CaptureEntityTypeConfiguration, add MatchedSkill index"
```

---

### Task 3: ICaptureRepository + EfCaptureRepository + EfCaptureService Refactor

**Files:**
- Create: `source/FlowHub.Core/Captures/ICaptureRepository.cs`
- Create: `source/FlowHub.Persistence/Repositories/EfCaptureRepository.cs`
- Modify: `source/FlowHub.Persistence/EfCaptureService.cs`
- Modify: `source/FlowHub.Persistence/PersistenceServiceCollectionExtensions.cs`
- Modify: `tests/FlowHub.Persistence.Tests/EfCaptureServiceTests.cs`

- [ ] **Step 1: Write the updated failing tests first**

Replace `tests/FlowHub.Persistence.Tests/EfCaptureServiceTests.cs` with the version that expects `ICaptureRepository` in `EfCaptureService`'s constructor. The tests will fail to compile until Steps 2–5 are complete.

```csharp
using FlowHub.Core.Captures;
using FlowHub.Core.Events;
using FlowHub.Persistence;
using MassTransit;

namespace FlowHub.Persistence.Tests;

public sealed class EfCaptureServiceTests
{
    private static (ICaptureRepository repo, EfCaptureService sut, IPublishEndpoint ep) Build()
    {
        var ep = Substitute.For<IPublishEndpoint>();
        var repo = Substitute.For<ICaptureRepository>();
        return (repo, new EfCaptureService(repo, ep), ep);
    }

    private static Capture MakeCapture(Guid? id = null, LifecycleStage stage = LifecycleStage.Raw,
        string? matchedSkill = null, string? failureReason = null) =>
        new(id ?? Guid.NewGuid(), ChannelKind.Web, "content", DateTimeOffset.UtcNow, stage, matchedSkill, failureReason);

    [Fact]
    public async Task SubmitAsync_RejectsEmptyContent()
    {
        var (_, sut, _) = Build();
        var act = () => sut.SubmitAsync("   ", ChannelKind.Web);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SubmitAsync_PublishesCaptureCreated()
    {
        var (repo, sut, ep) = Build();
        var returned = MakeCapture();
        repo.AddAsync(Arg.Any<Capture>(), Arg.Any<CancellationToken>()).Returns(returned);

        var result = await sut.SubmitAsync("https://example.com", ChannelKind.Web);

        result.Stage.Should().Be(LifecycleStage.Raw);
        await ep.Received(1).Publish(
            Arg.Is<CaptureCreated>(m => m.CaptureId == returned.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_DelegatesToRepository()
    {
        var (repo, sut, _) = Build();
        var id = Guid.NewGuid();
        var capture = MakeCapture(id);
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(capture);

        var result = await sut.GetByIdAsync(id);

        result.Should().Be(capture);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var (repo, sut, _) = Build();
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Capture?)null);

        var result = await sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRecentAsync_DelegatesToRepository()
    {
        var (repo, sut, _) = Build();
        IReadOnlyList<Capture> snapshot = [MakeCapture(), MakeCapture()];
        repo.GetRecentAsync(2, Arg.Any<CancellationToken>()).Returns(snapshot);

        var result = await sut.GetRecentAsync(2);

        result.Should().BeEquivalentTo(snapshot);
    }

    [Fact]
    public async Task GetFailureCountsAsync_DelegatesToRepository()
    {
        var (repo, sut, _) = Build();
        var counts = new FailureCounts(2, 1);
        repo.GetFailureCountsAsync(Arg.Any<CancellationToken>()).Returns(counts);

        var result = await sut.GetFailureCountsAsync();

        result.Should().Be(counts);
    }

    [Fact]
    public async Task ListAsync_DelegatesToRepository()
    {
        var (repo, sut, _) = Build();
        var filter = new CaptureFilter(null, null, 20, null);
        var page = new CapturePage([], null);
        repo.ListAsync(filter, Arg.Any<CancellationToken>()).Returns(page);

        var result = await sut.ListAsync(filter);

        result.Should().Be(page);
    }

    [Fact]
    public async Task MarkClassifiedAsync_UpdatesStageAndSkillAndTitle()
    {
        var (repo, sut, _) = Build();
        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeCapture(id));

        await sut.MarkClassifiedAsync(id, "Wallabag", "Some Title");

        await repo.Received(1).UpdateAsync(
            Arg.Is<Capture>(c =>
                c.Stage == LifecycleStage.Classified &&
                c.MatchedSkill == "Wallabag" &&
                c.Title == "Some Title"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkRoutedAsync_FlipsStageToRouted()
    {
        var (repo, sut, _) = Build();
        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeCapture(id, LifecycleStage.Classified, "Wallabag"));

        await sut.MarkRoutedAsync(id);

        await repo.Received(1).UpdateAsync(
            Arg.Is<Capture>(c => c.Stage == LifecycleStage.Routed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkCompletedAsync_PersistsExternalRef()
    {
        var (repo, sut, _) = Build();
        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeCapture(id, LifecycleStage.Routed, "Wallabag"));

        await sut.MarkCompletedAsync(id, "wal-42");

        await repo.Received(1).UpdateAsync(
            Arg.Is<Capture>(c => c.Stage == LifecycleStage.Completed && c.ExternalRef == "wal-42"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkOrphanAsync_PersistsFailureReason()
    {
        var (repo, sut, _) = Build();
        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeCapture(id));

        await sut.MarkOrphanAsync(id, "no skill matched");

        await repo.Received(1).UpdateAsync(
            Arg.Is<Capture>(c => c.Stage == LifecycleStage.Orphan && c.FailureReason == "no skill matched"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkUnhandledAsync_PersistsFailureReason()
    {
        var (repo, sut, _) = Build();
        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeCapture(id, LifecycleStage.Classified, "Wallabag"));

        await sut.MarkUnhandledAsync(id, "wallabag 503");

        await repo.Received(1).UpdateAsync(
            Arg.Is<Capture>(c => c.Stage == LifecycleStage.Unhandled && c.FailureReason == "wallabag 503"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResetForRetryAsync_ResetsStageAndClearsFailureReason()
    {
        var (repo, sut, _) = Build();
        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeCapture(id, LifecycleStage.Orphan, null, "no skill matched"));

        await sut.ResetForRetryAsync(id);

        await repo.Received(1).UpdateAsync(
            Arg.Is<Capture>(c => c.Stage == LifecycleStage.Raw && c.FailureReason == null),
            Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 2: Run — expect compile failure**

```bash
dotnet build FlowHub.slnx
```
Expected: Build errors — `ICaptureRepository` not found, `EfCaptureService` constructor mismatch.

- [ ] **Step 3: Create ICaptureRepository**

```csharp
// source/FlowHub.Core/Captures/ICaptureRepository.cs
namespace FlowHub.Core.Captures;

public interface ICaptureRepository
{
    Task<Capture?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Capture>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Capture>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
    Task<FailureCounts> GetFailureCountsAsync(CancellationToken cancellationToken = default);
    Task<Capture> AddAsync(Capture capture, CancellationToken cancellationToken = default);
    Task UpdateAsync(Capture capture, CancellationToken cancellationToken = default);
    Task<CapturePage> ListAsync(CaptureFilter filter, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 4: Create EfCaptureRepository**

```csharp
// source/FlowHub.Persistence/Repositories/EfCaptureRepository.cs
using FlowHub.Core.Captures;
using FlowHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowHub.Persistence.Repositories;

internal sealed class EfCaptureRepository : ICaptureRepository
{
    private readonly FlowHubDbContext _db;

    public EfCaptureRepository(FlowHubDbContext db) => _db = db;

    public async Task<Capture?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Captures.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<Capture>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _db.Captures.AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Capture>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        var entities = await _db.Captures.AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
        return entities.Select(ToDomain).ToList();
    }

    public async Task<FailureCounts> GetFailureCountsAsync(CancellationToken cancellationToken = default)
    {
        var orphan = await _db.Captures.AsNoTracking()
            .CountAsync(c => c.Stage == nameof(LifecycleStage.Orphan), cancellationToken);
        var unhandled = await _db.Captures.AsNoTracking()
            .CountAsync(c => c.Stage == nameof(LifecycleStage.Unhandled), cancellationToken);
        return new FailureCounts(orphan, unhandled);
    }

    public async Task<Capture> AddAsync(Capture capture, CancellationToken cancellationToken = default)
    {
        _db.Captures.Add(ToEntity(capture));
        await _db.SaveChangesAsync(cancellationToken);
        return capture;
    }

    public async Task UpdateAsync(Capture capture, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Captures.FirstOrDefaultAsync(c => c.Id == capture.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Capture {capture.Id} not found.");
        entity.Stage = capture.Stage.ToString();
        entity.MatchedSkill = capture.MatchedSkill;
        entity.Title = capture.Title;
        entity.FailureReason = capture.FailureReason;
        entity.ExternalRef = capture.ExternalRef;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CapturePage> ListAsync(CaptureFilter filter, CancellationToken cancellationToken = default)
    {
        IQueryable<CaptureEntity> query = _db.Captures.AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id);

        if (filter.Stages is { Count: > 0 } stages)
        {
            var stageStrings = stages.Select(s => s.ToString()).ToHashSet();
            query = query.Where(c => stageStrings.Contains(c.Stage));
        }

        if (filter.Source is ChannelKind src)
        {
            var sourceString = src.ToString();
            query = query.Where(c => c.Source == sourceString);
        }

        if (filter.Cursor is CaptureCursor cursor)
        {
            query = query.Where(c =>
                c.CreatedAt < cursor.CreatedAt
                || (c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) < 0));
        }

        var limit = Math.Clamp(filter.Limit, 1, 200);
        var fetched = await query.Take(limit + 1).ToListAsync(cancellationToken);

        if (fetched.Count > limit)
        {
            var last = fetched[limit - 1];
            return new CapturePage(
                fetched.Take(limit).Select(ToDomain).ToList(),
                new CaptureCursor(last.CreatedAt, last.Id));
        }

        return new CapturePage(fetched.Select(ToDomain).ToList(), null);
    }

    private static Capture ToDomain(CaptureEntity e) => new(
        Id: e.Id,
        Source: Enum.Parse<ChannelKind>(e.Source),
        Content: e.Content,
        CreatedAt: e.CreatedAt,
        Stage: Enum.Parse<LifecycleStage>(e.Stage),
        MatchedSkill: e.MatchedSkill,
        FailureReason: e.FailureReason,
        Title: e.Title,
        ExternalRef: e.ExternalRef);

    private static CaptureEntity ToEntity(Capture c) => new()
    {
        Id = c.Id,
        Content = c.Content,
        Source = c.Source.ToString(),
        Stage = c.Stage.ToString(),
        CreatedAt = c.CreatedAt,
        MatchedSkill = c.MatchedSkill,
        Title = c.Title,
        FailureReason = c.FailureReason,
        ExternalRef = c.ExternalRef,
    };
}
```

- [ ] **Step 5: Rewrite EfCaptureService to delegate to ICaptureRepository**

```csharp
// source/FlowHub.Persistence/EfCaptureService.cs
using FlowHub.Core.Captures;
using FlowHub.Core.Events;
using MassTransit;

namespace FlowHub.Persistence;

public sealed class EfCaptureService : ICaptureService
{
    private readonly ICaptureRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;

    public EfCaptureService(ICaptureRepository repository, IPublishEndpoint publishEndpoint)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
    }

    public Task<Capture?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _repository.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Capture>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public Task<IReadOnlyList<Capture>> GetRecentAsync(int count, CancellationToken cancellationToken = default) =>
        _repository.GetRecentAsync(count, cancellationToken);

    public Task<FailureCounts> GetFailureCountsAsync(CancellationToken cancellationToken = default) =>
        _repository.GetFailureCountsAsync(cancellationToken);

    public async Task<Capture> SubmitAsync(
        string content, ChannelKind source, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        var capture = new Capture(Guid.NewGuid(), source, content, DateTimeOffset.UtcNow, LifecycleStage.Raw, null);
        var saved = await _repository.AddAsync(capture, cancellationToken);
        await _publishEndpoint.Publish(
            new CaptureCreated(saved.Id, saved.Content, saved.Source, saved.CreatedAt),
            cancellationToken);
        return saved;
    }

    public async Task MarkClassifiedAsync(
        Guid id, string matchedSkill, string? title = null, CancellationToken cancellationToken = default)
    {
        var capture = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Capture {id} not found.");
        await _repository.UpdateAsync(
            capture with { Stage = LifecycleStage.Classified, MatchedSkill = matchedSkill, Title = title ?? capture.Title },
            cancellationToken);
    }

    public async Task MarkRoutedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var capture = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Capture {id} not found.");
        await _repository.UpdateAsync(capture with { Stage = LifecycleStage.Routed }, cancellationToken);
    }

    public async Task MarkCompletedAsync(Guid id, string? externalRef, CancellationToken cancellationToken = default)
    {
        var capture = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Capture {id} not found.");
        await _repository.UpdateAsync(
            capture with { Stage = LifecycleStage.Completed, ExternalRef = externalRef ?? capture.ExternalRef },
            cancellationToken);
    }

    public async Task MarkOrphanAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        var capture = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Capture {id} not found.");
        await _repository.UpdateAsync(
            capture with { Stage = LifecycleStage.Orphan, FailureReason = reason },
            cancellationToken);
    }

    public async Task MarkUnhandledAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        var capture = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Capture {id} not found.");
        await _repository.UpdateAsync(
            capture with { Stage = LifecycleStage.Unhandled, FailureReason = reason },
            cancellationToken);
    }

    public async Task ResetForRetryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var capture = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Capture {id} not found.");
        await _repository.UpdateAsync(
            capture with { Stage = LifecycleStage.Raw, FailureReason = null },
            cancellationToken);
    }

    public Task<CapturePage> ListAsync(CaptureFilter filter, CancellationToken cancellationToken = default) =>
        _repository.ListAsync(filter, cancellationToken);
}
```

- [ ] **Step 6: Register EfCaptureRepository in DI**

In `PersistenceServiceCollectionExtensions.AddFlowHubPersistence`, add before `EfCaptureService`:

```csharp
services.AddScoped<ICaptureRepository, EfCaptureRepository>();
services.AddScoped<ICaptureService, EfCaptureService>();
```

Also add the using: `using FlowHub.Persistence.Repositories;`

- [ ] **Step 7: Run tests — expect all to pass**

```bash
dotnet test FlowHub.slnx --filter "Category!=AI&Category!=BetaSmoke"
```
Expected: All `EfCaptureServiceTests` pass (12 tests). All other previously passing tests still pass.

- [ ] **Step 8: Commit**

```bash
git add source/FlowHub.Core/Captures/ICaptureRepository.cs \
  source/FlowHub.Persistence/Repositories/EfCaptureRepository.cs \
  source/FlowHub.Persistence/EfCaptureService.cs \
  source/FlowHub.Persistence/PersistenceServiceCollectionExtensions.cs \
  tests/FlowHub.Persistence.Tests/EfCaptureServiceTests.cs
git commit -m "refactor(persistence): introduce ICaptureRepository; EfCaptureService delegates to it"
```

---

### Task 4: Spec Docs

**Files:**
- Modify: `docs/spec/use-cases.md`
- Create: `docs/spec/nfa.md`
- Modify: `docs/spec/system-context.md`

- [ ] **Step 1: Extend use-cases.md with persistence use cases**

Append to `docs/spec/use-cases.md` (after the existing UCs):

```markdown
### UC-09: Filter Captures by Lifecycle Stage

**Actor:** Operator  
**Trigger:** Operator selects one or more stages in the Captures list filter.  
**Flow:** System queries `ICaptureService.ListAsync` with the selected stages. List refreshes to show only matching captures.  
**Acceptance:** Selecting "Orphan" returns only orphaned captures. Selecting multiple stages returns the union.

### UC-10: Filter Captures by Tag

**Actor:** Operator  
**Trigger:** Operator enters a tag value in the tag filter field.  
**Flow:** System queries `ICaptureService.ListAsync` with `CaptureFilter.Tag` set. Returns only captures that have the tag attached.  
**Acceptance:** A capture tagged "dotnet" appears when filter is "dotnet". Captures without the tag are excluded.

### UC-11: Search Captures by Content or Title

**Actor:** Operator  
**Trigger:** Operator types a search term in the search box.  
**Flow:** System queries with `CaptureFilter.SearchTerm`. PostgreSQL ILIKE match against Content and Title (case-insensitive). Not full-text search — deferred to Block 5.  
**Acceptance:** Searching "hexagonal" returns captures whose Content or Title contains that substring (any casing).

### UC-12: View Skill-Run History for a Capture

**Actor:** Operator  
**Trigger:** Operator opens a Capture detail and views the routing history tab.  
**Flow:** System queries `ISkillRunRepository.GetByCaptureIdAsync`. Returns all routing attempts for that capture ordered by StartedAt DESC.  
**Acceptance:** A capture that was routed twice shows two SkillRun entries.

### UC-13: View Integration Health History

**Actor:** Operator  
**Trigger:** Operator opens the Integrations page and expands an integration's history.  
**Flow:** System queries `IIntegrationRepository.GetRecentSamplesAsync`. Returns the last N health samples.  
**Acceptance:** Shows SampledAt, Status, and DurationMs for each sample.
```

- [ ] **Step 2: Create docs/spec/nfa.md**

```markdown
# Non-Functional Attributes (NfA) — SMART Criteria

## NfA-01: Query Latency

**Specific:** All Capture list queries (`ICaptureService.ListAsync`) with a limit ≤ 50 must complete within 100ms at p95 under normal load.  
**Measurable:** Measured via OpenTelemetry span duration on the `ListAsync` span; threshold surfaced in Grafana.  
**Achievable:** Index-backed queries on `Stage`, `CreatedAt`, and `MatchedSkill` columns; cursor pagination avoids full-table scans.  
**Relevant:** Dashboard and Captures list are the two highest-traffic read paths.  
**Time-bound:** Verified against Testcontainers PostgreSQL with 10k seeded rows in Slice 4.

## NfA-02: Index Strategy

All high-frequency filter columns carry dedicated B-tree indexes:

| Index | Column(s) | Query pattern |
|---|---|---|
| `IX_Captures_Stage` | `Stage` | Dashboard "Needs Attention", lifecycle filter |
| `IX_Captures_CreatedAt_DESC` | `CreatedAt DESC` | Recent Captures, cursor pagination |
| `IX_Captures_MatchedSkill` | `MatchedSkill` | Skill-based queries |
| `IX_IntegrationHealthSamples_IntegrationName_SampledAt_DESC` | `(IntegrationName, SampledAt DESC)` | Health history queries |

## NfA-03: Migration Strategy

- All schema changes via EF Core migrations (code-first, migration files committed to Git).
- Production apply: `dotnet ef migrations script --idempotent` generates an idempotent SQL script reviewed before each deploy.
- Never `EnsureCreated` or auto-migrate inside `app.Run()` in production — migrations run as a separate init step (12-Factor XII).
- Dev: `MigrationRunner` hosted service auto-applies on startup for developer convenience.

## NfA-04: Data Volume Assumptions

- Captures: up to 100,000 rows in Block 4 scope. Beyond 1M, consider partitioning (out of scope).
- IntegrationHealthSamples: up to 10,000 rows per integration. Prune policy (retain 90 days) deferred to Block 5.
- SkillRuns: up to 500,000 rows. Archival deferred to Block 5.

## NfA-05: Connection Resilience

Npgsql connection pool defaults (min=0, max=100) are sufficient for single-instance dev deployment. Production pool sizing configured via connection string parameters. Connection retries handled by Npgsql's built-in retry policy.
```

- [ ] **Step 3: Update Solution Vision in system-context.md**

In `docs/spec/system-context.md`, find or create a "Persistence Layer" subsection under Solution Vision and add:

```markdown
## Persistence Layer

FlowHub uses PostgreSQL 17 as its primary database, accessed via EF Core 10 with the Npgsql provider. The schema follows a migrations-first workflow: all schema changes are expressed as EF Core migration files committed to Git and applied as an idempotent SQL script at deploy time.

The Repository pattern separates domain logic from database access. Repository interfaces are defined in `FlowHub.Core` (returning domain types), with EF Core implementations in `FlowHub.Persistence`. Application-layer services (`ICaptureService`, `ISkillRegistry`, `IIntegrationHealthService`) compose repositories; they never reference `FlowHubDbContext` directly.

Local development uses `docker compose up postgres` to start a PostgreSQL container. `make db-migrate` applies pending migrations. `make run` starts the application, which auto-migrates via `MigrationRunner` for convenience.
```

- [ ] **Step 4: Commit**

```bash
git add docs/spec/use-cases.md docs/spec/nfa.md docs/spec/system-context.md
git commit -m "docs(spec): add persistence use cases, NfA SMART criteria, Solution Vision persistence paragraph"
```
