# SSRBusiness Conversion Quick Reference Guide

## Quick Navigation
- [VB.NET to C# Syntax](#vbnet-to-c-syntax)
- [LINQ to SQL → EF Core Patterns](#linq-to-sql--ef-core-patterns)
- [Common Pitfalls](#common-pitfalls)
- [Code Snippets](#code-snippets)

---

## VB.NET to C# Syntax

### Basic Syntax Conversions

| VB.NET | C# |
|--------|-----|
| `Public Class MyClass` | `public class MyClass` |
| `Private _field As String` | `private string _field;` |
| `Public Property Name As String` | `public string Name { get; set; }` |
| `Function GetValue() As Integer` | `int GetValue()` |
| `Sub DoSomething()` | `void DoSomething()` |
| `If condition Then` | `if (condition)` |
| `End If` | `}` |
| `For Each item In list` | `foreach (var item in list)` |
| `AndAlso` | `&&` |
| `OrElse` | `||` |
| `Not` | `!` |
| `Nothing` | `null` |
| `String.IsNullOrEmpty(x)` | `string.IsNullOrEmpty(x)` |
| `IsNot` | `!=` |

### Lambda Expressions

```vb
' VB.NET
Function(u) u.UserName = userName

' C#
u => u.UserName == userName
```

### LINQ Query Syntax

```vb
' VB.NET
Dim users = From u In Context.Users _
            Where u.Active = True _
            Order By u.UserName _
            Select u

' C# (Method Syntax - Preferred)
var users = Context.Users
    .Where(u => u.Active)
    .OrderBy(u => u.UserName);

' C# (Query Syntax - Alternative)
var users = from u in Context.Users
            where u.Active
            orderby u.UserName
            select u;
```

---

## LINQ to SQL → EF Core Patterns

### 1. Context Access

```vb
' VB.NET - LINQ to SQL
Public Class UserEntity
    Inherits SsrBusinessObject(Of User, SsrDataContext)
    
    Public Function GetUsers() As IQueryable(Of User)
        Return Me.Context.Users
    End Function
End Class
```

```csharp
// C# - EF Core
public class UserBusinessLogic : BusinessObjectBase<User>
{
    public UserBusinessLogic(SsrDbContext context) : base(context)
    {
    }
    
    public async Task<List<User>> GetUsersAsync()
    {
        return await Context.Users.ToListAsync();
    }
}
```

### 2. Loading Entities

```vb
' VB.NET - LINQ to SQL
Public Function Load(ByVal pk As Object) As TEntity
    Dim sql As String = "select * from " & Me.TableInfo.Tablename & 
                        " where " & Me.TableInfo.PkField & "={0}"
    Return Me.LoadBase(sql, pk)
End Function
```

```csharp
// C# - EF Core
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
```

### 3. Saving Entities

```vb
' VB.NET - LINQ to SQL
Public Overridable Function Save(ByVal entity As TEntity) As Boolean
    Using context As TContext = Me.CreateContext()
        Dim table As Table(Of TEntity) = context.GetTable(GetType(TEntity))
        Dim pKey As Object = entity.GetType().GetProperty(Me.TableInfo.PkField).GetValue(entity, Nothing)
        
        If pKey Is Nothing Then
            table.InsertOnSubmit(entity)
        Else
            table.Attach(entity, True)
        End If
        
        context.SubmitChanges()
    End Using
    Return True
End Function
```

```csharp
// C# - EF Core
public virtual async Task<bool> SaveAsync()
{
    if (!Validate())
        return false;

    try
    {
        // EF Core automatically tracks changes
        await Context.SaveChangesAsync();
        return true;
    }
    catch (Exception ex)
    {
        SetError(ex);
        return false;
    }
}
```

### 4. Deleting Entities

```vb
' VB.NET - LINQ to SQL
Public Function Delete(ByVal entity As TEntity) As Boolean
    Using context As TContext = Me.CreateContext()
        Dim table As Table(Of TEntity) = context.GetTable(GetType(TEntity))
        table.DeleteOnSubmit(entity)
        context.SubmitChanges()
    End Using
    Return True
End Function
```

```csharp
// C# - EF Core
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
```

### 5. Complex Queries with Joins

```vb
' VB.NET - LINQ to SQL
Public Function GetPermissionListByUser(ByVal userID As String) As IQueryable(Of Permission)
    Return (From p In Context.Permissions _
           Join rp In Context.RolePermissions On rp.PermissionCode Equals p.PermissionCode _
           Join r In Me.Context.Roles On r.RoleID Equals rp.RoleID _
           Join ur In Me.Context.UserRoles On ur.RoleID Equals r.RoleID _
           Join u In Me.Context.Users On u.UserID Equals ur.UserID _
           Where u.UserID = userID _
           Order By p.DisplayOrder, p.PermissionDesc.ToUpper() _
           Select p).Distinct
End Function
```

```csharp
// C# - EF Core (Using Navigation Properties)
public async Task<List<Permission>> GetPermissionListByUserAsync(int userId)
{
    return await Context.Permissions
        .Where(p => p.RolePermissions
            .Any(rp => rp.Role.UserRoles
                .Any(ur => ur.UserId == userId)))
        .OrderBy(p => p.DisplayOrder)
        .ThenBy(p => p.PermissionDesc)
        .Distinct()
        .ToListAsync();
}

// Alternative: Using explicit joins
public async Task<List<Permission>> GetPermissionListByUserAsync(int userId)
{
    return await (from p in Context.Permissions
                  join rp in Context.RolePermissions on p.PermissionCode equals rp.PermissionCode
                  join r in Context.Roles on rp.RoleId equals r.RoleId
                  join ur in Context.UserRoles on r.RoleId equals ur.RoleId
                  where ur.UserId == userId
                  orderby p.DisplayOrder, p.PermissionDesc
                  select p)
                  .Distinct()
                  .ToListAsync();
}
```

### 6. Eager Loading (Include)

```vb
' VB.NET - LINQ to SQL (Automatic lazy loading)
Dim user = Context.Users.SingleOrDefault(Function(u) u.UserID = userId)
' Navigation properties loaded automatically when accessed
Dim roles = user.UserRoles
```

```csharp
// C# - EF Core (Explicit loading)
var user = await Context.Users
    .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
    .FirstOrDefaultAsync(u => u.UserId == userId);
```

### 7. Checking Existence

```vb
' VB.NET - LINQ to SQL
Dim count As Integer = (From an In Me.Context.AcquisitionNotes _
                       Where an.UserID = userID _
                       Select an).Count
If count > 0 Then
    ' Has notes
End If
```

```csharp
// C# - EF Core (More efficient with Any)
var hasNotes = await Context.AcquisitionNotes
    .AnyAsync(an => an.UserId == userId);

if (hasNotes)
{
    // Has notes
}
```

---

## Common Pitfalls

### 1. Synchronous vs Asynchronous

❌ **Don't:**
```csharp
var users = Context.Users.ToList(); // Synchronous
```

✅ **Do:**
```csharp
var users = await Context.Users.ToListAsync(); // Asynchronous
```

### 2. N+1 Query Problem

❌ **Don't:**
```csharp
var users = await Context.Users.ToListAsync();
foreach (var user in users)
{
    // This causes a separate query for each user!
    var roles = await Context.UserRoles
        .Where(ur => ur.UserId == user.UserId)
        .ToListAsync();
}
```

✅ **Do:**
```csharp
var users = await Context.Users
    .Include(u => u.UserRoles)
    .ToListAsync();
```

### 3. Tracking vs No-Tracking

❌ **Don't:** (for read-only queries)
```csharp
var users = await Context.Users.ToListAsync(); // Tracked by default
```

✅ **Do:** (for read-only queries)
```csharp
var users = await Context.Users
    .AsNoTracking()
    .ToListAsync(); // Better performance for read-only
```

### 4. String Comparison

❌ **Don't:**
```csharp
.Where(u => u.UserName.ToUpper() == userName.ToUpper())
```

✅ **Do:**
```csharp
.Where(u => EF.Functions.Like(u.UserName, userName))
// or
.Where(u => u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase))
```

### 5. Null Reference Types

❌ **Don't:**
```csharp
public User Entity { get; set; } // Might be null
```

✅ **Do:**
```csharp
public User? Entity { get; set; } // Explicitly nullable
```

---

## Code Snippets

### Complete Entity Business Logic Template

```csharp
public class [EntityName]BusinessLogic : BusinessObjectBase<[EntityName]>
{
    public [EntityName]BusinessLogic(SsrDbContext context) : base(context)
    {
    }

    // Custom load methods
    public async Task<[EntityName]?> LoadBy[Property]Async([PropertyType] value)
    {
        Entity = await Context.[EntityName]s
            .FirstOrDefaultAsync(e => e.[Property] == value);
        return Entity;
    }

    // Custom query methods
    public async Task<List<[EntityName]>> Get[EntityName]ListAsync()
    {
        return await Context.[EntityName]s
            .OrderBy(e => e.[Property])
            .ToListAsync();
    }

    // Custom business logic
    public async Task<bool> Can[Action]Async(int id)
    {
        // Check business rules
        return await Context.[RelatedEntity]
            .AnyAsync(e => e.[ForeignKey] == id);
    }

    // Override validation
    protected override void CheckValidationRules()
    {
        if (Entity == null)
            return;

        if (string.IsNullOrEmpty(Entity.[Property]))
            ValidationErrors.Add("[Property] cannot be blank.");

        // Add more validation rules
    }

    // Override NewEntity if needed
    public override [EntityName] NewEntity()
    {
        var entity = base.NewEntity();
        
        // Set defaults
        entity.[Property] = [DefaultValue];
        
        return entity;
    }
}
```

### DbContext Configuration Template

```csharp
public class SsrDbContext : DbContext
{
    public SsrDbContext(DbContextOptions<SsrDbContext> options) 
        : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Acquisition> Acquisitions { get; set; } = null!;
    // ... more DbSets

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SsrDbContext).Assembly);

        // Or configure inline
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            
            entity.Property(e => e.UserName)
                .IsRequired()
                .HasMaxLength(35);
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.HasMany(e => e.UserRoles)
                .WithOne(ur => ur.User)
                .HasForeignKey(ur => ur.UserId);
        });
    }
}
```

### Entity Configuration Class Template

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(e => e.UserId);
        
        builder.Property(e => e.UserName)
            .IsRequired()
            .HasMaxLength(35);
        
        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(e => e.Password)
            .IsRequired()
            .HasMaxLength(40);
        
        builder.Property(e => e.Salt)
            .IsRequired()
            .HasMaxLength(40);
        
        // Relationships
        builder.HasMany(e => e.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        builder.HasIndex(e => e.UserName)
            .IsUnique();
    }
}
```

### Unit Test Template

```csharp
public class [EntityName]BusinessLogicTests
{
    private SsrDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SsrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new SsrDbContext(options);
    }

    [Fact]
    public async Task [MethodName]_[Scenario]_[ExpectedResult]()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var logic = new [EntityName]BusinessLogic(context);
        
        // Add test data
        var testEntity = new [EntityName]
        {
            [Property] = [Value]
        };
        context.[EntityName]s.Add(testEntity);
        await context.SaveChangesAsync();

        // Act
        var result = await logic.[MethodName]Async([Parameters]);

        // Assert
        Assert.NotNull(result);
        Assert.Equal([Expected], result.[Property]);
    }
}
```

---

## Conversion Checklist per Entity

- [ ] Create new C# business logic class
- [ ] Inherit from `BusinessObjectBase<TEntity>`
- [ ] Add constructor with `SsrDbContext` parameter
- [ ] Convert all custom query methods to async
- [ ] Update LINQ queries to use EF Core patterns
- [ ] Add `.ToListAsync()` or `.FirstOrDefaultAsync()` to queries
- [ ] Convert validation rules in `CheckValidationRules()`
- [ ] Update any custom save/delete logic
- [ ] Add XML documentation comments
- [ ] Create unit tests for all public methods
- [ ] Test with real database

---

## Performance Tips

1. **Use AsNoTracking for read-only queries:**
   ```csharp
   var users = await Context.Users.AsNoTracking().ToListAsync();
   ```

2. **Use projection for large result sets:**
   ```csharp
   var userNames = await Context.Users
       .Select(u => new { u.UserId, u.UserName })
       .ToListAsync();
   ```

3. **Batch operations:**
   ```csharp
   Context.Users.AddRange(newUsers);
   await Context.SaveChangesAsync();
   ```

4. **Use compiled queries for frequently executed queries:**
   ```csharp
   private static readonly Func<SsrDbContext, int, Task<User?>> GetUserById =
       EF.CompileAsyncQuery((SsrDbContext context, int id) =>
           context.Users.FirstOrDefault(u => u.UserId == id));
   ```

---

**Quick Reference Version:** 1.0  
**Last Updated:** 2025-12-20