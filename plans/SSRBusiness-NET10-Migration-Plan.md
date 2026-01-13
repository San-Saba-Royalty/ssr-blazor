# SSRBusiness Migration Plan: .NET Framework 3.5 to .NET 10

## Executive Summary

This document outlines the comprehensive migration strategy for converting the SSRBusiness VB.NET class library from .NET Framework 3.5 to .NET 10, including conversion from VB.NET to C# and migration from LINQ to SQL to Entity Framework Core.

**Project:** SSRBusiness (SSRBusinessRules)  
**Current State:** .NET Framework 3.5, VB.NET, LINQ to SQL  
**Target State:** .NET 10, C#, Entity Framework Core  
**Migration Type:** Full rewrite with modernization  

---

## Current Project Analysis

### Technology Stack
- **Framework:** .NET Framework 3.5
- **Language:** Visual Basic .NET
- **Data Access:** LINQ to SQL (SsrDbModel.dbml)
- **Database:** SQL Server (SanSaba database)
- **Project Type:** Class Library
- **Key Dependencies:**
  - System.Data.Linq
  - System.Data.SqlClient
  - System.Configuration
  - System.Web (for some utilities)

### Project Structure
```
SSRBusiness/
├── BusinessClasses/
│   ├── LoginManager.vb
│   └── Support/
│       ├── App.vb
│       └── SaltedHash.vb
├── BusinessFramework/
│   ├── DataContextFactory.vb
│   ├── QueryConverter.vb
│   ├── SsrBusinessObject.vb (Base class)
│   ├── ValidationErrors.vb
│   └── Support/
│       └── DynamicQuery.vb
├── Entities/ (50+ entity classes)
│   ├── UserEntity.vb
│   ├── AcquisitionEntity.vb
│   └── [Many more...]
├── Model/
│   ├── SsrDataContext.vb
│   ├── SsrDbModel.dbml (LINQ to SQL model)
│   └── SsrDbModel.designer.vb
├── ReportQueries/
│   └── [Various report query classes]
└── My Project/
    ├── AssemblyInfo.vb
    ├── Application.Designer.vb
    ├── Resources.Designer.vb
    └── Settings.Designer.vb
```

### Key Components Analysis

#### 1. SsrBusinessObject<TEntity, TContext>
**Purpose:** Generic base class for all business objects  
**Key Features:**
- CRUD operations (Load, Save, Delete, NewEntity)
- Change tracking (Connected/Disconnected modes)
- Validation framework
- Error handling
- Query conversion utilities

**Dependencies:**
- System.Data.Linq.DataContext
- System.Data.Linq.Table<T>
- LINQ to SQL metadata

#### 2. Entity Classes (50+ files)
**Pattern:** Each entity inherits from `SsrBusinessObject<EntityType, SsrDataContext>`  
**Example:** `UserEntity : SsrBusinessObject<User, SsrDataContext>`  
**Features:**
- Custom query methods
- Business logic
- Validation rules
- Authentication (UserEntity)

#### 3. LINQ to SQL Model
**Database Tables:** 40+ tables including:
- Users, Roles, Permissions
- Acquisitions, LetterAgreements
- Counties, Operators, Buyers
- Various lookup tables

**Relationships:** Complex foreign key relationships with navigation properties

---

## Migration Strategy

### Phase 1: Project Setup and Infrastructure

#### 1.1 Create New .NET 10 C# Project
```bash
dotnet new classlib -n SSRBusiness -f net10.0 -lang C#
```

**New Project Structure:**
```
SSRBusiness/
├── SSRBusiness.csproj (SDK-style)
├── Data/
│   ├── SsrDbContext.cs (EF Core DbContext)
│   └── Configurations/ (Entity configurations)
├── Entities/
│   └── [EF Core entity classes]
├── BusinessLogic/
│   ├── Base/
│   │   └── BusinessObjectBase.cs
│   └── [Business logic classes]
├── Models/
│   └── [DTOs and view models]
├── Validation/
│   └── ValidationErrorCollection.cs
└── Utilities/
    └── SaltedHash.cs
```

#### 1.2 Install Required NuGet Packages
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.0" />
<PackageReference Include="System.ComponentModel.Annotations" Version="6.0.0" />
```

### Phase 2: Database and Entity Framework Core Setup

#### 2.1 Scaffold EF Core Entities from Database
```bash
dotnet ef dbcontext scaffold "Server=DSNAME;Database=SanSaba;User Id=sa;Password=***;" Microsoft.EntityFrameworkCore.SqlServer -o Entities -c SsrDbContext --context-dir Data --force
```

**Configuration Options:**
- Use Data Annotations for simple validations
- Use Fluent API for complex relationships
- Generate navigation properties
- Use nullable reference types

#### 2.2 Customize DbContext
```csharp
public class SsrDbContext : DbContext
{
    public SsrDbContext(DbContextOptions<SsrDbContext> options) 
        : base(options)
    {
    }

    // DbSet properties for each entity
    public DbSet<User> Users { get; set; }
    public DbSet<Acquisition> Acquisitions { get; set; }
    // ... 40+ more DbSets

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SsrDbContext).Assembly);
        
        // Custom configurations
        ConfigureUsers(modelBuilder);
        ConfigureAcquisitions(modelBuilder);
        // ... more configurations
    }
}
```

### Phase 3: Convert Base Business Object Class

#### 3.1 Migrate SsrBusinessObject to BusinessObjectBase

**Key Changes:**
1. Replace `DataContext` with `DbContext`
2. Replace `Table<T>` with `DbSet<T>`
3. Update change tracking mechanism
4. Modernize error handling
5. Remove version field requirement (use EF Core concurrency tokens)

**New C# Implementation:**
```csharp
public abstract class BusinessObjectBase<TEntity> where TEntity : class, new()
{
    protected SsrDbContext Context { get; }
    public TEntity? Entity { get; set; }
    public ValidationErrorCollection ValidationErrors { get; }
    public Exception? ErrorException { get; set; }
    public string ErrorMessage => ErrorException?.Message ?? string.Empty;
    
    protected BusinessObjectBase(SsrDbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        ValidationErrors = new ValidationErrorCollection();
    }

    public virtual async Task<TEntity?> LoadAsync(object id)
    {
        try
        {
            Entity = await Context.Set<TEntity>().FindAsync(id);
            return Entity;
        }
        catch (Exception ex)
        {
            SetError(ex);
            return null;
        }
    }

    public virtual TEntity NewEntity()
    {
        Entity = new TEntity();
        Context.Set<TEntity>().Add(Entity);
        return Entity;
    }

    public virtual async Task<bool> SaveAsync()
    {
        if (!Validate())
            return false;

        try
        {
            await Context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            SetError(ex);
            return false;
        }
        catch (Exception ex)
        {
            SetError(ex);
            return false;
        }
    }

    public virtual async Task<bool> DeleteAsync()
    {
        if (Entity == null)
            return false;

        try
        {
            Context.Set<TEntity>().Remove(Entity);
            await Context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            SetError(ex);
            return false;
        }
    }

    protected virtual bool Validate()
    {
        ValidationErrors.Clear();
        CheckValidationRules();
        return ValidationErrors.Count == 0;
    }

    protected virtual void CheckValidationRules() { }

    protected void SetError(Exception? ex = null)
    {
        ErrorException = ex;
    }
}
```

### Phase 4: Convert Entity Business Logic Classes

#### 4.1 UserEntity Conversion Example

**VB.NET Original:**
```vb
Public Class UserEntity
    Inherits SsrBusinessObject(Of User, SsrDataContext)
    
    Public Function LoadUserByUserName(ByVal userName As String) As User
        Me.Entity = Me.Context.Users.SingleOrDefault(Function(u) u.UserName = userName)
        Return Me.Entity
    End Function
End Class
```

**C# Converted:**
```csharp
public class UserBusinessLogic : BusinessObjectBase<User>
{
    public UserBusinessLogic(SsrDbContext context) : base(context)
    {
    }

    public async Task<User?> LoadUserByUserNameAsync(string userName)
    {
        Entity = await Context.Users
            .FirstOrDefaultAsync(u => u.UserName == userName);
        return Entity;
    }

    public async Task<User?> AuthenticateAndLoadAsync(string userName, string password)
    {
        var user = await LoadUserByUserNameAsync(userName);
        if (user == null)
            return null;

        var saltedHash = SaltedHash.Create(user.Salt, user.Password);
        return saltedHash.Verify(password) ? user : null;
    }

    public async Task<List<User>> GetUserListAsync()
    {
        return await Context.Users
            .OrderBy(u => u.UserName)
            .ToListAsync();
    }

    public async Task<bool> CanDeleteUserAsync(int userId)
    {
        var hasAcquisitionNotes = await Context.AcquisitionNotes
            .AnyAsync(an => an.UserId == userId);
        var hasAcquisitionChanges = await Context.AcquisitionChanges
            .AnyAsync(ac => ac.UserId == userId);
        var hasAcquisitionDocuments = await Context.AcquisitionDocuments
            .AnyAsync(ad => ad.UserId == userId);
        var hasAcquisitions = await Context.Acquisitions
            .AnyAsync(a => a.LandManId == userId);

        return !hasAcquisitionNotes && !hasAcquisitionChanges && 
               !hasAcquisitionDocuments && !hasAcquisitions;
    }

    protected override void CheckValidationRules()
    {
        if (Entity == null)
            return;

        if (string.IsNullOrEmpty(Entity.UserName))
            ValidationErrors.Add("User name cannot be blank.");

        if (string.IsNullOrEmpty(Entity.Password))
            ValidationErrors.Add("Password cannot be blank.");

        if (string.IsNullOrEmpty(Entity.Email))
            ValidationErrors.Add("Email cannot be blank.");
        else if (!IsValidEmail(Entity.Email))
            ValidationErrors.Add("Email must be a valid email address.");
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
```

### Phase 5: Configuration and Connection Management

#### 5.1 Modern Configuration Pattern

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "SanSabaConnection": "Server=DSNAME;Database=SanSaba;User Id=sa;Password=***;TrustServerCertificate=true"
  },
  "ApplicationSettings": {
    "PasswordExpirationDays": 90,
    "PasswordRequiresAlphaNumeric": true,
    "PasswordRequiresSpecial": true,
    "PasswordRequiresUpperCase": true,
    "NumberPasswordsStored": 5,
    "EnableLockoutDueToFailedPassword": true,
    "NumberAttemptsBeforeLockout": 3,
    "NumberMinutesToLockout": 30
  }
}
```

#### 5.2 Dependency Injection Setup

**ServiceCollectionExtensions.cs:**
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSSRBusiness(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<SsrDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Register business logic classes
        services.AddScoped<UserBusinessLogic>();
        services.AddScoped<AcquisitionBusinessLogic>();
        // ... register all business logic classes

        return services;
    }
}
```

### Phase 6: Key Migration Patterns

#### 6.1 LINQ to SQL → EF Core Query Patterns

**Pattern 1: Simple Queries**
```csharp
// LINQ to SQL (VB.NET)
Dim users = From u In Context.Users Where u.Active = True Select u

// EF Core (C#)
var users = await Context.Users.Where(u => u.Active).ToListAsync();
```

**Pattern 2: Joins**
```csharp
// LINQ to SQL (VB.NET)
Dim query = From u In Context.Users _
            Join ur In Context.UserRoles On u.UserID Equals ur.UserID _
            Select u

// EF Core (C#)
var query = await Context.Users
    .Include(u => u.UserRoles)
    .ToListAsync();
```

**Pattern 3: Complex Queries**
```csharp
// LINQ to SQL (VB.NET)
Dim result = (From p In Context.Permissions _
              Join rp In Context.RolePermissions On p.PermissionCode Equals rp.PermissionCode _
              Where rp.RoleID = roleId _
              Select p).Distinct()

// EF Core (C#)
var result = await Context.Permissions
    .Where(p => p.RolePermissions.Any(rp => rp.RoleId == roleId))
    .Distinct()
    .ToListAsync();
```

#### 6.2 Change Tracking Patterns

**LINQ to SQL Attach Pattern:**
```vb
table.Attach(entity, True)
```

**EF Core Update Pattern:**
```csharp
Context.Entry(entity).State = EntityState.Modified;
// or
Context.Update(entity);
```

#### 6.3 Concurrency Handling

**LINQ to SQL (Version Field):**
```vb
' Automatic with timestamp field
```

**EF Core (Concurrency Token):**
```csharp
[Timestamp]
public byte[]? RowVersion { get; set; }

// In OnModelCreating
modelBuilder.Entity<User>()
    .Property(u => u.RowVersion)
    .IsRowVersion();
```

### Phase 7: Testing Strategy

#### 7.1 Unit Tests Structure
```
SSRBusiness.Tests/
├── BusinessLogic/
│   ├── UserBusinessLogicTests.cs
│   ├── AcquisitionBusinessLogicTests.cs
│   └── [More test classes]
├── Utilities/
│   └── SaltedHashTests.cs
└── TestHelpers/
    ├── InMemoryDbContextFactory.cs
    └── TestDataBuilder.cs
```

#### 7.2 Sample Unit Test
```csharp
public class UserBusinessLogicTests
{
    private SsrDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SsrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new SsrDbContext(options);
    }

    [Fact]
    public async Task LoadUserByUserName_ExistingUser_ReturnsUser()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var testUser = new User 
        { 
            UserName = "testuser",
            Email = "test@example.com",
            Password = "hashedpassword",
            Salt = "salt"
        };
        context.Users.Add(testUser);
        await context.SaveChangesAsync();

        var userLogic = new UserBusinessLogic(context);

        // Act
        var result = await userLogic.LoadUserByUserNameAsync("testuser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("testuser", result.UserName);
    }

    [Fact]
    public async Task Validate_EmptyUserName_AddsValidationError()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var userLogic = new UserBusinessLogic(context);
        userLogic.Entity = new User { UserName = "", Email = "test@example.com" };

        // Act
        var isValid = userLogic.Validate();

        // Assert
        Assert.False(isValid);
        Assert.Contains(userLogic.ValidationErrors, 
            e => e.Message.Contains("User name cannot be blank"));
    }
}
```

#### 7.3 Integration Tests
```csharp
public class UserBusinessLogicIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public UserBusinessLogicIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SaveAsync_NewUser_InsertsIntoDatabase()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var userLogic = new UserBusinessLogic(context);
        var newUser = userLogic.NewEntity();
        newUser.UserName = "newuser";
        newUser.Email = "new@example.com";
        newUser.Password = "password123";

        // Act
        var result = await userLogic.SaveAsync();

        // Assert
        Assert.True(result);
        Assert.True(newUser.UserId > 0);
    }
}
```

### Phase 8: Breaking Changes Documentation

#### 8.1 API Changes

| Old API (VB.NET) | New API (C#) | Notes |
|------------------|--------------|-------|
| `Load(pk)` | `LoadAsync(id)` | Now async |
| `Save()` | `SaveAsync()` | Now async |
| `Delete()` | `DeleteAsync()` | Now async |
| `Context.SubmitChanges()` | `Context.SaveChangesAsync()` | EF Core method |
| `Table<T>` | `DbSet<T>` | EF Core type |
| `DataContext` | `DbContext` | EF Core base class |
| `SingleOrDefault()` | `FirstOrDefaultAsync()` | Async LINQ |

#### 8.2 Behavioral Changes

1. **Change Tracking:**
   - Old: Manual attach/detach with version fields
   - New: Automatic EF Core change tracking

2. **Lazy Loading:**
   - Old: Automatic with LINQ to SQL
   - New: Must explicitly enable or use `.Include()`

3. **Validation:**
   - Old: Custom validation framework
   - New: Can integrate with Data Annotations

4. **Connection Management:**
   - Old: Connection string in app.config
   - New: Dependency injection with options pattern

### Phase 9: Migration Execution Plan

#### Week 1: Setup and Infrastructure
- [ ] Create new .NET 10 C# project
- [ ] Install NuGet packages
- [ ] Scaffold EF Core entities
- [ ] Set up testing infrastructure

#### Week 2: Core Framework
- [ ] Convert BusinessObjectBase class
- [ ] Convert ValidationErrorCollection
- [ ] Convert SaltedHash utility
- [ ] Convert DataContextFactory

#### Week 3-4: Entity Conversion (Batch 1)
- [ ] Convert User-related entities (5 classes)
- [ ] Convert Acquisition-related entities (15 classes)
- [ ] Write unit tests for converted classes

#### Week 5-6: Entity Conversion (Batch 2)
- [ ] Convert LetterAgreement-related entities (15 classes)
- [ ] Convert lookup entities (10 classes)
- [ ] Write unit tests for converted classes

#### Week 7: Entity Conversion (Batch 3)
- [ ] Convert remaining entities (15 classes)
- [ ] Convert report query classes
- [ ] Write unit tests for converted classes

#### Week 8: Integration and Testing
- [ ] Integration testing with real database
- [ ] Performance testing
- [ ] Fix any issues discovered

#### Week 9: Documentation and Deployment
- [ ] Update API documentation
- [ ] Create migration guide for consumers
- [ ] Deploy to test environment

#### Week 10: Final Testing and Release
- [ ] User acceptance testing
- [ ] Fix final issues
- [ ] Production deployment

---

## Risk Assessment

### High Risk Items
1. **Data Loss:** Incorrect EF Core configuration could cause data issues
   - **Mitigation:** Extensive testing with database backups

2. **Performance Degradation:** EF Core queries might be slower
   - **Mitigation:** Performance profiling and query optimization

3. **Breaking Changes:** Consumers of the library will need updates
   - **Mitigation:** Comprehensive documentation and migration guide

### Medium Risk Items
1. **Complex Query Translation:** Some LINQ to SQL queries may not translate directly
   - **Mitigation:** Manual query optimization and raw SQL where needed

2. **Concurrency Conflicts:** Different concurrency handling in EF Core
   - **Mitigation:** Thorough testing of concurrent operations

### Low Risk Items
1. **VB.NET to C# Syntax:** Well-documented conversion patterns
2. **Configuration Changes:** Standard .NET migration patterns

---

## Success Criteria

1. ✅ All 50+ entity classes converted to C#
2. ✅ All business logic functional with EF Core
3. ✅ 80%+ code coverage with unit tests
4. ✅ All integration tests passing
5. ✅ Performance within 10% of original
6. ✅ Zero data corruption issues
7. ✅ Complete API documentation
8. ✅ Migration guide for consumers

---

## Post-Migration Improvements

### Immediate Opportunities
1. **Async/Await Throughout:** All I/O operations now async
2. **Dependency Injection:** Modern DI patterns
3. **Nullable Reference Types:** Better null safety
4. **Records for DTOs:** Immutable data transfer objects
5. **Pattern Matching:** Modern C# features

### Future Enhancements
1. **Repository Pattern:** Abstract EF Core further
2. **CQRS:** Separate read/write operations
3. **Specification Pattern:** Reusable query logic
4. **Domain Events:** Better decoupling
5. **GraphQL Support:** Modern API patterns

---

## Appendix

### A. Key File Mappings

| Old VB.NET File | New C# File | Notes |
|-----------------|-------------|-------|
| `SsrBusinessObject.vb` | `BusinessObjectBase.cs` | Core base class |
| `UserEntity.vb` | `UserBusinessLogic.cs` | Business logic |
| `SsrDataContext.vb` | `SsrDbContext.cs` | EF Core context |
| `ValidationErrors.vb` | `ValidationErrorCollection.cs` | Validation |
| `SaltedHash.vb` | `SaltedHash.cs` | Utility |

### B. Reference Documentation
- [EF Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [.NET 10 Migration Guide](https://docs.microsoft.com/en-us/dotnet/core/migration/)
- [VB.NET to C# Conversion Guide](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/)

### C. Tools and Resources
- **Conversion Tools:** Telerik Code Converter, SharpDevelop
- **Testing Tools:** xUnit, Moq, FluentAssertions
- **Profiling Tools:** dotTrace, BenchmarkDotNet
- **Database Tools:** SQL Server Management Studio, EF Core CLI

---

**Document Version:** 1.0  
**Last Updated:** 2025-12-20  
**Author:** Migration Planning Team