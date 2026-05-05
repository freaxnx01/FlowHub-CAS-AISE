# Block 4 — Slice 4: Tests + Docker (May 25–Jun 10)

**Rubric targets:** Abnahmekriterien (5), Test-Strategie (5), Unit-Tests (3), Test-Ergebnisse (3), Sub-Systeme als Container (5)

**Prerequisites:** Slice 3 complete. All 3 migrations applied. Docker available.

---

### Task 13: Testcontainers Setup

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `tests/FlowHub.Persistence.Tests/FlowHub.Persistence.Tests.csproj`
- Create: `tests/FlowHub.Persistence.Tests/Fixtures/PostgresFixture.cs`

- [ ] **Step 1: Add Testcontainers packages to Directory.Packages.props**

In the `Label="Persistence"` group (or a new `Label="Testcontainers"` group):

```xml
<ItemGroup Label="Testcontainers">
  <PackageVersion Include="Testcontainers" Version="3.10.0" />
  <PackageVersion Include="Testcontainers.PostgreSql" Version="3.10.0" />
  <PackageVersion Include="Npgsql" Version="9.0.4" />
</ItemGroup>
```

Also add to the existing Persistence group (if not already present from Task 1):

```xml
<PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.4" />
```

- [ ] **Step 2: Update FlowHub.Persistence.Tests.csproj**

Replace the project file:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>FlowHub.Persistence.Tests</RootNamespace>
    <AssemblyName>FlowHub.Persistence.Tests</AssemblyName>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <NoWarn>$(NoWarn);CA1707;CA1861</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="Npgsql" />
    <PackageReference Include="Testcontainers.PostgreSql" />
    <PackageReference Include="MassTransit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\source\FlowHub.Core\FlowHub.Core.csproj" />
    <ProjectReference Include="..\..\source\FlowHub.Persistence\FlowHub.Persistence.csproj" />
  </ItemGroup>

</Project>
```

Note: `Microsoft.EntityFrameworkCore.InMemory` is removed — all persistence tests now use a real PostgreSQL container.

- [ ] **Step 3: Create PostgresFixture**

```csharp
// tests/FlowHub.Persistence.Tests/Fixtures/PostgresFixture.cs
using FlowHub.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FlowHub.Persistence.Tests.Fixtures;

public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    public string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithDatabase("flowhub_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public async Task<FlowHubDbContext> CreateFreshDbAsync()
    {
        var dbName = $"testdb_{Guid.NewGuid():N}";

        await using var adminConn = new NpgsqlConnection(ConnectionString);
        await adminConn.OpenAsync();
        await using var cmd = adminConn.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE \"{dbName}\"";
        await cmd.ExecuteNonQueryAsync();

        var csb = new NpgsqlConnectionStringBuilder(ConnectionString) { Database = dbName };
        var options = new DbContextOptionsBuilder<FlowHubDbContext>()
            .UseNpgsql(csb.ConnectionString)
            .Options;
        var db = new FlowHubDbContext(options);
        await db.Database.MigrateAsync();
        return db;
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
```

- [ ] **Step 4: Build**

```bash
dotnet build FlowHub.slnx
```
Expected: 0 errors.

---

### Task 14: EfCaptureRepositoryTests

**Files:**
- Create: `tests/FlowHub.Persistence.Tests/Repositories/EfCaptureRepositoryTests.cs`

- [ ] **Step 1: Write the tests**

```csharp
// tests/FlowHub.Persistence.Tests/Repositories/EfCaptureRepositoryTests.cs
using FlowHub.Core.Captures;
using FlowHub.Persistence.Repositories;
using FlowHub.Persistence.Tests.Fixtures;

namespace FlowHub.Persistence.Tests.Repositories;

[Collection(PostgresCollection.Name)]
public sealed class EfCaptureRepositoryTests(PostgresFixture fixture)
{
    private static Capture NewRawCapture(string content = "content", ChannelKind source = ChannelKind.Web) =>
        new(Guid.NewGuid(), source, content, DateTimeOffset.UtcNow, LifecycleStage.Raw, null);

    [Fact]
    public async Task AddAsync_PersistsCapture_AndReturnsDomainObject()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        var capture = NewRawCapture("https://example.com");

        var saved = await repo.AddAsync(capture);

        saved.Id.Should().Be(capture.Id);
        saved.Content.Should().Be("https://example.com");
        saved.Stage.Should().Be(LifecycleStage.Raw);
        saved.Source.Should().Be(ChannelKind.Web);

        var found = await repo.GetByIdAsync(capture.Id);
        found.Should().NotBeNull();
        found!.Id.Should().Be(capture.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCaptures_OrderedByCreatedAtDesc()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        var old = NewRawCapture("old") with { CreatedAt = DateTimeOffset.UtcNow.AddHours(-1) };
        var recent = NewRawCapture("recent") with { CreatedAt = DateTimeOffset.UtcNow };
        await repo.AddAsync(old);
        await repo.AddAsync(recent);

        var all = await repo.GetAllAsync();

        all.Should().HaveCount(2);
        all[0].Content.Should().Be("recent");
        all[1].Content.Should().Be("old");
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsTopNByCreatedAtDesc()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        for (var i = 0; i < 5; i++)
        {
            await repo.AddAsync(NewRawCapture($"item-{i}") with
            {
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }

        var result = await repo.GetRecentAsync(3);

        result.Should().HaveCount(3);
        result[0].Content.Should().Be("item-0");
    }

    [Fact]
    public async Task GetFailureCountsAsync_CountsOrphanAndUnhandled()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        var a = NewRawCapture("a");
        var b = NewRawCapture("b");
        var c = NewRawCapture("c");
        await repo.AddAsync(a);
        await repo.AddAsync(b);
        await repo.AddAsync(c);
        await repo.UpdateAsync(a with { Stage = LifecycleStage.Orphan, FailureReason = "x" });
        await repo.UpdateAsync(b with { Stage = LifecycleStage.Orphan, FailureReason = "x" });
        await repo.UpdateAsync(c with { Stage = LifecycleStage.Unhandled, FailureReason = "y" });

        var counts = await repo.GetFailureCountsAsync();

        counts.OrphanCount.Should().Be(2);
        counts.UnhandledCount.Should().Be(1);
    }

    [Fact]
    public async Task ListAsync_FiltersByStage()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        var raw = NewRawCapture("raw");
        var orphan = NewRawCapture("orphan");
        await repo.AddAsync(raw);
        await repo.AddAsync(orphan);
        await repo.UpdateAsync(orphan with { Stage = LifecycleStage.Orphan, FailureReason = "x" });

        var page = await repo.ListAsync(new CaptureFilter(
            Stages: [LifecycleStage.Orphan],
            Source: null,
            Limit: 50,
            Cursor: null));

        page.Items.Should().HaveCount(1);
        page.Items[0].Content.Should().Be("orphan");
    }

    [Fact]
    public async Task ListAsync_FiltersBySource()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        await repo.AddAsync(NewRawCapture("web", ChannelKind.Web));
        await repo.AddAsync(NewRawCapture("api", ChannelKind.Api));

        var page = await repo.ListAsync(new CaptureFilter(
            Stages: null,
            Source: ChannelKind.Api,
            Limit: 50,
            Cursor: null));

        page.Items.Should().HaveCount(1);
        page.Items[0].Content.Should().Be("api");
    }

    [Fact]
    public async Task ListAsync_CursorPagination_ReturnsNextCursor()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        for (var i = 0; i < 5; i++)
        {
            await repo.AddAsync(NewRawCapture($"item-{i}") with
            {
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }

        var page1 = await repo.ListAsync(new CaptureFilter(null, null, 3, null));

        page1.Items.Should().HaveCount(3);
        page1.Next.Should().NotBeNull();

        var page2 = await repo.ListAsync(new CaptureFilter(null, null, 3, page1.Next));

        page2.Items.Should().HaveCount(2);
        page2.Next.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_AppliesAllFieldChanges()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        var original = NewRawCapture("original");
        await repo.AddAsync(original);

        await repo.UpdateAsync(original with
        {
            Stage = LifecycleStage.Classified,
            MatchedSkill = "Wallabag",
            Title = "Some Title",
        });

        var updated = await repo.GetByIdAsync(original.Id);
        updated!.Stage.Should().Be(LifecycleStage.Classified);
        updated.MatchedSkill.Should().Be("Wallabag");
        updated.Title.Should().Be("Some Title");
    }
}
```

- [ ] **Step 2: Run tests**

```bash
dotnet test tests/FlowHub.Persistence.Tests --filter "FullyQualifiedName~EfCaptureRepositoryTests"
```
Expected: 8 tests pass. Docker must be running (Testcontainers starts its own postgres container automatically).

---

### Task 15: EfSkillRegistryTests + EfIntegrationHealthServiceTests

**Files:**
- Create: `tests/FlowHub.Persistence.Tests/Repositories/EfSkillRegistryTests.cs`
- Create: `tests/FlowHub.Persistence.Tests/Repositories/EfIntegrationHealthServiceTests.cs`

- [ ] **Step 1: Create EfSkillRegistryTests**

```csharp
// tests/FlowHub.Persistence.Tests/Repositories/EfSkillRegistryTests.cs
using FlowHub.Core.Health;
using FlowHub.Persistence;
using FlowHub.Persistence.Repositories;
using FlowHub.Persistence.Tests.Fixtures;

namespace FlowHub.Persistence.Tests.Repositories;

[Collection(PostgresCollection.Name)]
public sealed class EfSkillRegistryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task GetHealthAsync_ReturnsAllSkills()
    {
        var db = await fixture.CreateFreshDbAsync();
        var skillRepo = new EfSkillRepository(db);
        await skillRepo.UpsertAsync(new SkillHealth("Wallabag", HealthStatus.Healthy, 5));
        await skillRepo.UpsertAsync(new SkillHealth("Vikunja", HealthStatus.Degraded, 2));
        var sut = new EfSkillRegistry(skillRepo);

        var result = await sut.GetHealthAsync();

        result.Should().HaveCount(2);
        result.Should().ContainSingle(s => s.Name == "Wallabag" && s.Status == HealthStatus.Healthy && s.RoutedToday == 5);
        result.Should().ContainSingle(s => s.Name == "Vikunja" && s.Status == HealthStatus.Degraded && s.RoutedToday == 2);
    }

    [Fact]
    public async Task GetHealthAsync_EmptyDb_ReturnsEmptyList()
    {
        var db = await fixture.CreateFreshDbAsync();
        var sut = new EfSkillRegistry(new EfSkillRepository(db));

        var result = await sut.GetHealthAsync();

        result.Should().BeEmpty();
    }
}
```

- [ ] **Step 2: Create EfIntegrationHealthServiceTests**

```csharp
// tests/FlowHub.Persistence.Tests/Repositories/EfIntegrationHealthServiceTests.cs
using FlowHub.Core.Health;
using FlowHub.Persistence;
using FlowHub.Persistence.Repositories;
using FlowHub.Persistence.Tests.Fixtures;

namespace FlowHub.Persistence.Tests.Repositories;

[Collection(PostgresCollection.Name)]
public sealed class EfIntegrationHealthServiceTests(PostgresFixture fixture)
{
    [Fact]
    public async Task GetHealthAsync_ReturnsAllIntegrations()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfIntegrationRepository(db);
        await repo.UpsertAsync(new IntegrationHealth("Wallabag", HealthStatus.Healthy, DateTimeOffset.UtcNow.AddMinutes(-1), TimeSpan.FromMilliseconds(180)));
        var sut = new EfIntegrationHealthService(repo);

        var result = await sut.GetHealthAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Wallabag");
        result[0].Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task GetHealthAsync_MapsDurationMs_ToTimeSpan()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfIntegrationRepository(db);
        await repo.UpsertAsync(new IntegrationHealth("Vikunja", HealthStatus.Healthy, null, TimeSpan.FromMilliseconds(250)));
        var sut = new EfIntegrationHealthService(repo);

        var result = await sut.GetHealthAsync();

        result[0].LastWriteDuration.Should().Be(TimeSpan.FromMilliseconds(250));
    }

    [Fact]
    public async Task GetHealthAsync_EmptyDb_ReturnsEmptyList()
    {
        var db = await fixture.CreateFreshDbAsync();
        var sut = new EfIntegrationHealthService(new EfIntegrationRepository(db));

        var result = await sut.GetHealthAsync();

        result.Should().BeEmpty();
    }
}
```

- [ ] **Step 3: Run tests**

```bash
dotnet test tests/FlowHub.Persistence.Tests \
  --filter "FullyQualifiedName~EfSkillRegistryTests|FullyQualifiedName~EfIntegrationHealthServiceTests"
```
Expected: 5 tests pass.

---

### Task 16: Migration Smoke Test

**Files:**
- Create: `tests/FlowHub.Persistence.Tests/Migrations/MigrationSmokeTest.cs`

- [ ] **Step 1: Write the smoke test**

```csharp
// tests/FlowHub.Persistence.Tests/Migrations/MigrationSmokeTest.cs
using FlowHub.Persistence;
using FlowHub.Persistence.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace FlowHub.Persistence.Tests.Migrations;

[Collection(PostgresCollection.Name)]
public sealed class MigrationSmokeTest(PostgresFixture fixture)
{
    private static readonly string[] ExpectedTables =
    [
        "Captures", "Channels", "Skills", "SkillRuns",
        "Integrations", "IntegrationHealthSamples", "Tags"
    ];

    [Fact]
    public async Task ApplyAllMigrations_AllExpectedTablesExist()
    {
        var db = await fixture.CreateFreshDbAsync();

        foreach (var table in ExpectedTables)
        {
            var count = await db.Database
                .SqlQueryRaw<int>(
                    $"SELECT COUNT(*)::int FROM information_schema.tables " +
                    $"WHERE table_schema='public' AND table_name='{table.ToLower()}'")
                .SingleAsync();

            count.Should().Be(1, $"table '{table}' should exist after migration");
        }
    }

    [Fact]
    public async Task ApplyMigrations_Idempotent_WhenCalledTwice()
    {
        var db = await fixture.CreateFreshDbAsync();

        var act = async () => await db.Database.MigrateAsync();

        await act.Should().NotThrowAsync("second MigrateAsync call should be a no-op");
    }
}
```

- [ ] **Step 2: Run tests**

```bash
dotnet test tests/FlowHub.Persistence.Tests --filter "FullyQualifiedName~MigrationSmokeTest"
```
Expected: 2 tests pass.

- [ ] **Step 3: Run full test suite**

```bash
dotnet test FlowHub.slnx --filter "Category!=AI&Category!=BetaSmoke"
```
Expected: All passing. Note the total test count and record it in CHANGELOG.

---

### Task 17: Docker Compose Full Stack + Testing Strategy Doc

**Files:**
- Modify: `docker-compose.yml`
- Modify: `Makefile`
- Modify: `docs/spec/testing-strategy.md`
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Update docker-compose.yml with full stack**

Replace `docker-compose.yml` with:

```yaml
version: "3.8"

services:
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

  flowhub.migrations:
    image: mcr.microsoft.com/dotnet/sdk:10.0
    working_dir: /app
    volumes:
      - .:/app
    command: >
      sh -c "dotnet tool restore &&
             dotnet ef database update
             --project source/FlowHub.Persistence
             --startup-project source/FlowHub.Web"
    environment:
      ConnectionStrings__Default: "Host=postgres;Port=5432;Database=flowhub;Username=flowhub;Password=dev-secret"
    depends_on:
      postgres:
        condition: service_healthy

  flowhub.web:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "5070:5070"
    environment:
      ASPNETCORE_URLS: "http://+:5070"
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__Default: "Host=postgres;Port=5432;Database=flowhub;Username=flowhub;Password=dev-secret"
      Bus__Transport: RabbitMq
    depends_on:
      flowhub.migrations:
        condition: service_completed_successfully

  rabbitmq:
    image: rabbitmq:3-management-alpine
    ports:
      - "5672:5672"
      - "15672:15672"
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  postgres_data:
```

Note: `flowhub.migrations` uses the SDK image to run EF migrations before `flowhub.web` starts — this is the 12-Factor XII pattern. A `Dockerfile` at repo root is required for `flowhub.web` (out of scope for Block 4; Block 5 will deliver it). Comment out `flowhub.web` service if Dockerfile does not exist yet.

- [ ] **Step 2: Verify db-up + db-migrate work against Compose postgres**

```bash
make db-up
# Expected: postgres container starts, health check passes

make db-migrate
# Expected: "Done." — migrations applied successfully
```

- [ ] **Step 3: Update docs/spec/testing-strategy.md — add Persistence Layer section**

Append to `docs/spec/testing-strategy.md`:

```markdown
## Persistence Layer Testing

### Why Testcontainers Instead of InMemory

The existing `EfCaptureServiceTests` previously used `Microsoft.EntityFrameworkCore.InMemory`. This was replaced with Testcontainers PostgreSQL starting in Block 4 for these reasons:

1. **Provider parity:** InMemory does not enforce FK constraints, does not support `EF.Functions.ILike`, and has different behavior for edge cases in cursor pagination. A test that passes against InMemory can fail against PostgreSQL.
2. **Realistic query plans:** Index-backed queries behave differently under InMemory (no query optimizer). Tests against real PostgreSQL catch missing indexes early.
3. **Migration coverage:** InMemory doesn't apply migrations. `MigrationSmokeTest` verifies all migrations apply cleanly against a real engine.

### Test Infrastructure

`PostgresFixture` (`tests/FlowHub.Persistence.Tests/Fixtures/PostgresFixture.cs`) starts one PostgreSQL container per test session (shared via `[Collection("Postgres")]`). Each test gets its own database (`testdb_<guid>`) via `CreateFreshDbAsync()`, which creates the database and applies all migrations before returning the context.

### Test Pyramid for Persistence

| Level | Location | Provider | Count (Block 4) |
|---|---|---|---|
| Unit (service orchestration) | `FlowHub.Web.ComponentTests` + `FlowHub.Persistence.Tests` | NSubstitute mocks | ~20 |
| Integration (repository) | `FlowHub.Persistence.Tests` | Testcontainers PostgreSQL | ~15 |
| Integration (API endpoints) | `FlowHub.Api.IntegrationTests` | Testcontainers PostgreSQL | planned Block 5 |
| E2E (browser) | Playwright (planned) | Full stack | planned Block 5 |

### Acceptance Criteria (UC-09 to UC-13)

| Use Case | Test | Location |
|---|---|---|
| UC-09 Filter by Stage | `ListAsync_FiltersByStage` | `EfCaptureRepositoryTests` |
| UC-10 Filter by Tag | `ListAsync_FiltersByTag` (add in Block 5) | — |
| UC-11 Search by Content | `ListAsync_FiltersBySearchTerm` (add in Block 5) | — |
| UC-12 SkillRun history | `GetByCaptureIdAsync` | `EfSkillRunRepository` (add test in Block 5) |
| UC-13 Integration health history | `GetRecentSamplesAsync` | `EfIntegrationRepository` (add test in Block 5) |
```

- [ ] **Step 4: Update CHANGELOG.md [Unreleased] section**

In `CHANGELOG.md`, under `## [Unreleased]`, add:

```markdown
### Added
- PostgreSQL provider (Npgsql) replaces SQLite throughout `FlowHub.Persistence`
- `ICaptureRepository` + `EfCaptureRepository` — repository interface in Core, EF Core impl in Persistence
- `IChannelRepository` + `EfChannelRepository`, `ISkillRepository` + `EfSkillRepository`
- `IIntegrationRepository` + `EfIntegrationRepository`, `ITagRepository` + `EfTagRepository`
- `ISkillRunRepository` + `EfSkillRunRepository`
- `EfSkillRegistry` (replaces `SkillRegistryStub` in DI), `EfIntegrationHealthService` (replaces `IntegrationHealthServiceStub` in DI)
- `CaptureQueryBuilder` — expression-tree filter supporting Stage, Source, Tag, SearchTerm (ILike), cursor
- EF Core migrations: `0001_Initial` (PostgreSQL), `0002_AddChannelAndSkill`, `0003_Block4FullDomain`
- Testcontainers PostgreSQL test infrastructure (`PostgresFixture`, per-test isolated databases)
- `EfCaptureRepositoryTests` (8 tests), `EfSkillRegistryTests` (2 tests), `EfIntegrationHealthServiceTests` (3 tests), `MigrationSmokeTest` (2 tests) — total 15 new integration tests
- Docker Compose full stack: `postgres`, `flowhub.migrations` init service, `flowhub.web`
- `make db-up` and `make db-migrate` Makefile targets
- `docs/spec/nfa.md` — SMART NfA criteria
- `docs/design/db/er.md` — full ER diagram (Mermaid)

### Changed
- `EfCaptureService` now delegates all data access to `ICaptureRepository` (no direct DbContext dependency)
- `EfCaptureServiceTests` updated to use NSubstitute mocks for `ICaptureRepository`
- `CaptureFilter` extended with `Tag` and `SearchTerm` optional fields

### Removed
- `SkillRegistryStub` removed from DI (class kept for component tests)
- `IntegrationHealthServiceStub` removed from DI (class kept for component tests)
- SQLite migration `20260504120638_Initial` (replaced by PostgreSQL-compatible migrations)

### Test Results
- Total tests (Block 4): XX passing, 0 failing (run `make test` to verify current count)
- New persistence integration tests: 15 (all via Testcontainers PostgreSQL)
```

- [ ] **Step 5: Commit**

```bash
git add docker-compose.yml Makefile docs/spec/testing-strategy.md CHANGELOG.md
git commit -m "feat(infra): Docker Compose full stack with postgres + migrations service; update test strategy docs"
```
