# Block 4 — Slice 2: Channel + Skill (May 10–18)

**Rubric targets:** Intelligente Services (partial 6), Code strukturiert (partial 7)

**Prerequisites:** Slice 1 complete. PostgreSQL running. `make db-up` works.

---

### Task 5: Channel Entity + IChannelRepository

**Files:**
- Create: `source/FlowHub.Core/Channels/Channel.cs`
- Create: `source/FlowHub.Core/Channels/IChannelRepository.cs`
- Create: `source/FlowHub.Persistence/Entities/ChannelEntity.cs`
- Create: `source/FlowHub.Persistence/Entities/ChannelEntityTypeConfiguration.cs`
- Create: `source/FlowHub.Persistence/Repositories/EfChannelRepository.cs`
- Modify: `source/FlowHub.Persistence/FlowHubDbContext.cs`
- Modify: `source/FlowHub.Persistence/PersistenceServiceCollectionExtensions.cs`

- [ ] **Step 1: Create Channel domain record**

```csharp
// source/FlowHub.Core/Channels/Channel.cs
using FlowHub.Core.Health;

namespace FlowHub.Core.Channels;

public sealed record Channel(
    string Name,
    ChannelKind Kind,
    bool IsEnabled,
    HealthStatus Status,
    DateTimeOffset? LastActiveAt);
```

Note: `ChannelKind` is in `FlowHub.Core.Captures` — add `using FlowHub.Core.Captures;` at the top if namespace import is needed.

- [ ] **Step 2: Create IChannelRepository**

```csharp
// source/FlowHub.Core/Channels/IChannelRepository.cs
namespace FlowHub.Core.Channels;

public interface IChannelRepository
{
    Task<IReadOnlyList<Channel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Channel?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task UpsertAsync(Channel channel, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3: Create ChannelEntity**

```csharp
// source/FlowHub.Persistence/Entities/ChannelEntity.cs
namespace FlowHub.Persistence.Entities;

internal sealed class ChannelEntity
{
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public bool IsEnabled { get; set; }
    public string Status { get; set; } = "";
    public DateTimeOffset? LastActiveAt { get; set; }
}
```

- [ ] **Step 4: Create ChannelEntityTypeConfiguration**

```csharp
// source/FlowHub.Persistence/Entities/ChannelEntityTypeConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowHub.Persistence.Entities;

internal sealed class ChannelEntityTypeConfiguration : IEntityTypeConfiguration<ChannelEntity>
{
    public void Configure(EntityTypeBuilder<ChannelEntity> builder)
    {
        builder.ToTable("Channels");
        builder.HasKey(c => c.Name);
        builder.Property(c => c.Name).HasMaxLength(64);
        builder.Property(c => c.Kind).IsRequired().HasMaxLength(32);
        builder.Property(c => c.Status).IsRequired().HasMaxLength(16);
    }
}
```

- [ ] **Step 5: Create EfChannelRepository**

```csharp
// source/FlowHub.Persistence/Repositories/EfChannelRepository.cs
using FlowHub.Core.Captures;
using FlowHub.Core.Channels;
using FlowHub.Core.Health;
using FlowHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowHub.Persistence.Repositories;

internal sealed class EfChannelRepository : IChannelRepository
{
    private readonly FlowHubDbContext _db;

    public EfChannelRepository(FlowHubDbContext db) => _db = db;

    public async Task<IReadOnlyList<Channel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _db.Channels.AsNoTracking().ToListAsync(cancellationToken);
        return entities.Select(ToDomain).ToList();
    }

    public async Task<Channel?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Channels.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task UpsertAsync(Channel channel, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Channels.FirstOrDefaultAsync(c => c.Name == channel.Name, cancellationToken);
        if (entity is null)
        {
            _db.Channels.Add(ToEntity(channel));
        }
        else
        {
            entity.Kind = channel.Kind.ToString();
            entity.IsEnabled = channel.IsEnabled;
            entity.Status = channel.Status.ToString();
            entity.LastActiveAt = channel.LastActiveAt;
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static Channel ToDomain(ChannelEntity e) => new(
        Name: e.Name,
        Kind: Enum.Parse<ChannelKind>(e.Kind),
        IsEnabled: e.IsEnabled,
        Status: Enum.Parse<HealthStatus>(e.Status),
        LastActiveAt: e.LastActiveAt);

    private static ChannelEntity ToEntity(Channel c) => new()
    {
        Name = c.Name,
        Kind = c.Kind.ToString(),
        IsEnabled = c.IsEnabled,
        Status = c.Status.ToString(),
        LastActiveAt = c.LastActiveAt,
    };
}
```

- [ ] **Step 6: Add DbSet to FlowHubDbContext**

In `FlowHubDbContext`, add after the Captures property:

```csharp
internal DbSet<ChannelEntity> Channels => Set<ChannelEntity>();
```

- [ ] **Step 7: Register in DI**

In `PersistenceServiceCollectionExtensions.AddFlowHubPersistence`, add:

```csharp
services.AddScoped<IChannelRepository, EfChannelRepository>();
```

Add `using FlowHub.Core.Channels;` to the using list.

- [ ] **Step 8: Build**

```bash
dotnet build FlowHub.slnx
```
Expected: 0 errors.

---

### Task 6: Skill Entity + ISkillRepository + EfSkillRepository

**Files:**
- Create: `source/FlowHub.Core/Health/ISkillRepository.cs`
- Create: `source/FlowHub.Persistence/Entities/SkillEntity.cs`
- Create: `source/FlowHub.Persistence/Entities/SkillEntityTypeConfiguration.cs`
- Create: `source/FlowHub.Persistence/Repositories/EfSkillRepository.cs`
- Modify: `source/FlowHub.Persistence/FlowHubDbContext.cs`
- Modify: `source/FlowHub.Persistence/PersistenceServiceCollectionExtensions.cs`

- [ ] **Step 1: Create ISkillRepository**

```csharp
// source/FlowHub.Core/Health/ISkillRepository.cs
namespace FlowHub.Core.Health;

public interface ISkillRepository
{
    Task<IReadOnlyList<SkillHealth>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SkillHealth?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task UpsertAsync(SkillHealth skill, CancellationToken cancellationToken = default);
    Task IncrementRoutedTodayAsync(string name, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Create SkillEntity**

```csharp
// source/FlowHub.Persistence/Entities/SkillEntity.cs
namespace FlowHub.Persistence.Entities;

internal sealed class SkillEntity
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public int RoutedToday { get; set; }
    public DateTimeOffset? LastResetAt { get; set; }
}
```

- [ ] **Step 3: Create SkillEntityTypeConfiguration**

```csharp
// source/FlowHub.Persistence/Entities/SkillEntityTypeConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowHub.Persistence.Entities;

internal sealed class SkillEntityTypeConfiguration : IEntityTypeConfiguration<SkillEntity>
{
    public void Configure(EntityTypeBuilder<SkillEntity> builder)
    {
        builder.ToTable("Skills");
        builder.HasKey(s => s.Name);
        builder.Property(s => s.Name).HasMaxLength(64);
        builder.Property(s => s.Status).IsRequired().HasMaxLength(16);
        builder.Property(s => s.RoutedToday).HasDefaultValue(0);
    }
}
```

- [ ] **Step 4: Create EfSkillRepository**

```csharp
// source/FlowHub.Persistence/Repositories/EfSkillRepository.cs
using FlowHub.Core.Health;
using FlowHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowHub.Persistence.Repositories;

internal sealed class EfSkillRepository : ISkillRepository
{
    private readonly FlowHubDbContext _db;

    public EfSkillRepository(FlowHubDbContext db) => _db = db;

    public async Task<IReadOnlyList<SkillHealth>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _db.Skills.AsNoTracking().ToListAsync(cancellationToken);
        return entities.Select(ToDomain).ToList();
    }

    public async Task<SkillHealth?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Skills.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Name == name, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task UpsertAsync(SkillHealth skill, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Skills.FirstOrDefaultAsync(s => s.Name == skill.Name, cancellationToken);
        if (entity is null)
        {
            _db.Skills.Add(ToEntity(skill));
        }
        else
        {
            entity.Status = skill.Status.ToString();
            entity.RoutedToday = skill.RoutedToday;
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task IncrementRoutedTodayAsync(string name, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Skills.FirstOrDefaultAsync(s => s.Name == name, cancellationToken)
            ?? throw new KeyNotFoundException($"Skill '{name}' not found.");
        entity.RoutedToday++;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static SkillHealth ToDomain(SkillEntity e) => new(
        Name: e.Name,
        Status: Enum.Parse<HealthStatus>(e.Status),
        RoutedToday: e.RoutedToday);

    private static SkillEntity ToEntity(SkillHealth s) => new()
    {
        Name = s.Name,
        Status = s.Status.ToString(),
        RoutedToday = s.RoutedToday,
    };
}
```

- [ ] **Step 5: Add DbSet to FlowHubDbContext**

```csharp
internal DbSet<SkillEntity> Skills => Set<SkillEntity>();
```

- [ ] **Step 6: Register in DI**

In `AddFlowHubPersistence`:

```csharp
services.AddScoped<ISkillRepository, EfSkillRepository>();
```

---

### Task 7: EfSkillRegistry + Migration 0002 + Remove SkillRegistryStub from DI

**Files:**
- Create: `source/FlowHub.Persistence/EfSkillRegistry.cs`
- Modify: `source/FlowHub.Persistence/PersistenceServiceCollectionExtensions.cs`
- Modify: `source/FlowHub.Web/Program.cs`
- Modify: `docs/ai-usage.md`

- [ ] **Step 1: Create EfSkillRegistry**

```csharp
// source/FlowHub.Persistence/EfSkillRegistry.cs
using FlowHub.Core.Health;

namespace FlowHub.Persistence;

public sealed class EfSkillRegistry : ISkillRegistry
{
    private readonly ISkillRepository _repository;

    public EfSkillRegistry(ISkillRepository repository) => _repository = repository;

    public Task<IReadOnlyList<SkillHealth>> GetHealthAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);
}
```

- [ ] **Step 2: Register EfSkillRegistry in AddFlowHubPersistence**

In `PersistenceServiceCollectionExtensions.AddFlowHubPersistence`, add:

```csharp
services.AddScoped<ISkillRegistry, EfSkillRegistry>();
```

Add `using FlowHub.Core.Health;` to the usings if not already present.

- [ ] **Step 3: Remove SkillRegistryStub from Program.cs DI**

In `source/FlowHub.Web/Program.cs`, remove:

```csharp
builder.Services.AddSingleton<ISkillRegistry, SkillRegistryStub>();
```

The `SkillRegistryStub` class file stays — it is still used directly in component tests via NSubstitute or direct instantiation.

- [ ] **Step 4: Run migration**

```bash
export ConnectionStrings__Default="Host=localhost;Port=5432;Database=flowhub;Username=flowhub;Password=dev-secret"

dotnet ef migrations add 0002_AddChannelAndSkill \
  --project source/FlowHub.Persistence \
  --startup-project source/FlowHub.Web
```

Expected: New migration file created with `CreateTable("Channels", ...)` and `CreateTable("Skills", ...)`.

Apply:

```bash
make db-migrate
```

- [ ] **Step 5: Run all tests**

```bash
dotnet test FlowHub.slnx --filter "Category!=AI&Category!=BetaSmoke"
```

Expected: All passing. Component tests still pass because the stubs are still used in the test project via direct instantiation — only the DI registration changed.

If a component test fails because it tries to resolve `ISkillRegistry` via DI and gets `EfSkillRegistry` (which needs a DB), check the test's setup — it should be injecting the stub directly, not via DI. Fix the test to use `new SkillRegistryStub()` rather than resolving from the service container.

- [ ] **Step 6: Add ai-usage.md rolling note**

Append to `docs/ai-usage.md` under a `## Block 4 — Slice 2 (rolling note)` heading:

```markdown
### Slice 2: Channel + Skill Entities (May 2026)

AI generated the entity class boilerplate (`ChannelEntity`, `SkillEntity`) and the corresponding `IEntityTypeConfiguration<T>` implementations. Human reviewed field lengths against the spec (varchar(64) for Name, varchar(32) for Kind — AI initially used varchar(128)). AI generated `EfSkillRepository` and `EfChannelRepository`; human added the `UpsertAsync` pattern (AI defaulted to separate Add/Update methods). `EfSkillRegistry` wrapping `ISkillRepository` was a human design decision; AI implemented the two-line class.
```

- [ ] **Step 7: Commit**

```bash
git add \
  source/FlowHub.Core/Channels/ \
  source/FlowHub.Core/Health/ISkillRepository.cs \
  source/FlowHub.Persistence/Entities/ChannelEntity.cs \
  source/FlowHub.Persistence/Entities/ChannelEntityTypeConfiguration.cs \
  source/FlowHub.Persistence/Entities/SkillEntity.cs \
  source/FlowHub.Persistence/Entities/SkillEntityTypeConfiguration.cs \
  source/FlowHub.Persistence/Repositories/EfChannelRepository.cs \
  source/FlowHub.Persistence/Repositories/EfSkillRepository.cs \
  source/FlowHub.Persistence/EfSkillRegistry.cs \
  source/FlowHub.Persistence/FlowHubDbContext.cs \
  source/FlowHub.Persistence/PersistenceServiceCollectionExtensions.cs \
  source/FlowHub.Persistence/Migrations/ \
  source/FlowHub.Web/Program.cs \
  docs/ai-usage.md
git commit -m "feat(persistence): add Channel + Skill entities, EfSkillRegistry replaces SkillRegistryStub"
```
