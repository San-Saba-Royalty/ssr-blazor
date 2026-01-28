# Letter Agreement Column Persistence Implementation Plan

## Overview
Implement persistent column visibility settings for the LetterAgreementIndex page using the existing ViewService infrastructure with performance-optimized caching.

## Current Architecture Understanding

### Database Schema
```
DisplayField (defines available columns per module)
├── FieldID (PK)
├── FieldName (e.g., "SellerLastName")
├── ColumnName (e.g., "Seller Last Name")
├── Module (e.g., "LetterAgreement")
└── DisplayOrder

View (user-defined column sets)
├── ViewID (PK)
├── ViewName (e.g., "My Custom View", "Default")
└── Module (e.g., "LetterAgreement")

ViewField (junction table: which fields are in which view)
├── ViewFieldID (PK)
├── ViewID (FK)
├── FieldID (FK)
└── DisplayOrder

UserPagePreference (user's preferred view per page)
├── PreferenceID (PK)
├── UserID (FK)
├── PageName (e.g., "LetterAgreementIndex")
└── ViewID (FK)
```

### Existing Services
- **ViewRepository**: CRUD operations for Views, ViewFields, UserPagePreferences
- **DisplayFieldRepository**: Read operations for DisplayFields
- **ViewService**: Business logic layer wrapping repositories
- **CachedDataService<T>**: Generic in-memory caching service (currently entity-focused)

## Implementation Plan

### Phase 1: Database Setup
#### 1.1 Create DisplayFields for LetterAgreement Module
Create a migration to seed DisplayField records for all 19 Letter Agreement columns:

```sql
INSERT INTO DisplayFields (FieldName, ColumnName, DisplayOrder, Module) VALUES
-- Always visible
('LetterAgreementID', 'ID', 1, 'LetterAgreement'),
('BankingDays', 'Banking Days', 2, 'LetterAgreement'),
('AcquisitionID', 'Acq ID', 3, 'LetterAgreement'),

-- Seller Info (hidden by default)
('SellerLastName', 'Seller Last Name', 10, 'LetterAgreement'),
('SellerName', 'Seller Name', 11, 'LetterAgreement'),
('SellerEmail', 'Seller Email', 12, 'LetterAgreement'),
('SellerPhone', 'Seller Phone', 13, 'LetterAgreement'),
('SellerCity', 'Seller City', 14, 'LetterAgreement'),
('SellerState', 'Seller State', 15, 'LetterAgreement'),
('SellerZipCode', 'Seller Zip Code', 16, 'LetterAgreement'),

-- Dates (hidden by default)
('CreatedOn', 'Created On', 20, 'LetterAgreement'),
('EffectiveDate', 'Effective Date', 21, 'LetterAgreement'),

-- Financial (hidden by default)
('TotalBonus', 'Purchase Price', 30, 'LetterAgreement'),
('ConsiderationFee', 'Consideration Fee', 31, 'LetterAgreement'),
('ReferralFee', 'Referral Fee', 32, 'LetterAgreement'),

-- Acreage (hidden by default)
('TotalGrossAcres', 'Total Gross Acres', 40, 'LetterAgreement'),
('TotalNetAcres', 'Total Net Acres', 41, 'LetterAgreement'),

-- Other (hidden by default)
('LandMan', 'Land Man', 50, 'LetterAgreement'),
('DealStatus', 'Status', 51, 'LetterAgreement'),
('CountyName', 'County Name', 52, 'LetterAgreement'),
('OperatorName', 'Operator Name', 53, 'LetterAgreement'),
('UnitName', 'Unit Name', 54, 'LetterAgreement');
```

#### 1.2 Create Default View
Create a default "Standard" view for LetterAgreement module with only the 3 always-visible columns.

### Phase 2: Enhanced Caching Service
#### 2.1 Create ViewCacheService
Create a new specialized caching service for View-related data:

**Location**: `Services/ViewCacheService.cs`

**Responsibilities**:
- Cache all Views per module
- Cache all DisplayFields per module
- Cache UserPagePreferences (user-specific, loaded on login)
- Provide fast lookup methods
- Handle cache invalidation on updates

**Key Methods**:
```csharp
Task<List<View>> GetViewsForModuleAsync(string module)
Task<List<DisplayField>> GetDisplayFieldsForModuleAsync(string module)
Task<ViewConfiguration?> GetUserViewForPageAsync(string userId, string pageName, string module)
Task SaveUserViewPreferenceAsync(string userId, string pageName, ViewConfiguration viewConfig)
Task InvalidateUserCacheAsync(string userId)
Task WarmCacheAsync() // Called on startup
```

**Cache Structure**:
```csharp
Dictionary<string, List<View>> _viewsByModule
Dictionary<string, List<DisplayField>> _displayFieldsByModule
Dictionary<string, Dictionary<string, int>> _userPagePreferences // userId -> (pageName -> viewId)
```

### Phase 3: Application Startup Cache Warming
#### 3.1 Create IHostedService for Cache Initialization

**Location**: `Services/CacheWarmingService.cs`

```csharp
public class CacheWarmingService : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var viewCacheService = scope.ServiceProvider.GetRequiredService<ViewCacheService>();
        await viewCacheService.WarmCacheAsync();
    }
}
```

**Register in DependencyInjection.cs**:
```csharp
services.AddHostedService<CacheWarmingService>();
services.AddSingleton<ViewCacheService>();
```

### Phase 4: User Login Cache Initialization
#### 4.1 Update Account Login Logic

**Location**: Modify existing login flow (likely in `AccountController` or authentication handler)

**After successful authentication**:
```csharp
var viewCacheService = serviceProvider.GetRequiredService<ViewCacheService>();
await viewCacheService.LoadUserPreferencesAsync(userId);
```

### Phase 5: Update LetterAgreementIndex Component
#### 5.1 Inject Required Services

Add to `LetterAgreementIndex.razor.cs`:
```csharp
[Inject] private ViewCacheService ViewCacheService { get; set; } = null!;
[Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
```

#### 5.2 Load User's View Preference on Initialize

```csharp
protected override async Task OnInitializedAsync()
{
    await LoadUserViewPreferenceAsync();
}

private async Task LoadUserViewPreferenceAsync()
{
    var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
    var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    if (!string.IsNullOrEmpty(userId))
    {
        var viewConfig = await ViewCacheService.GetUserViewForPageAsync(
            userId, 
            "LetterAgreementIndex", 
            "LetterAgreement"
        );
        
        if (viewConfig != null)
        {
            // Map ViewConfiguration fields to ColumnVisibilityState
            _columnVisibility = MapViewConfigToColumnVisibility(viewConfig);
        }
    }
}
```

#### 5.3 Save Column Visibility Changes

Update `ApplyColumnVisibility` method:
```csharp
private async Task ApplyColumnVisibility()
{
    _showColumnChooser = false;
    
    var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
    var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    if (!string.IsNullOrEmpty(userId))
    {
        // Convert ColumnVisibilityState to ViewConfiguration
        var viewConfig = MapColumnVisibilityToViewConfig(_columnVisibility);
        
        // Save via cache service (which persists to DB and updates cache)
        await ViewCacheService.SaveUserViewPreferenceAsync(
            userId, 
            "LetterAgreementIndex", 
            viewConfig
        );
        
        Snackbar.Add("Column preferences saved", Severity.Success);
    }
    
    // Force grid re-render
    if (_dataGrid != null)
    {
        await _dataGrid.ReloadServerData();
    }
}
```

#### 5.4 Add Mapping Helper Methods

```csharp
private ColumnVisibilityState MapViewConfigToColumnVisibility(ViewConfiguration viewConfig)
{
    var visibility = new ColumnVisibilityState();
    var selectedFields = viewConfig.Fields.Where(f => f.IsSelected).Select(f => f.FieldName).ToHashSet();
    
    visibility.SellerLastName = selectedFields.Contains("SellerLastName");
    visibility.SellerName = selectedFields.Contains("SellerName");
    // ... map all fields
    
    return visibility;
}

private ViewConfiguration MapColumnVisibilityToViewConfig(ColumnVisibilityState visibility)
{
    // Get all DisplayFields for LetterAgreement module from cache
    var allFields = await ViewCacheService.GetDisplayFieldsForModuleAsync("LetterAgreement");
    
    var viewConfig = new ViewConfiguration
    {
        Module = "LetterAgreement",
        ViewName = "User Custom View", // or get/create named view
        Fields = allFields.Select((field, index) => new ViewFieldSelection
        {
            FieldID = field.FieldID,
            FieldName = field.FieldName,
            DisplayName = field.ColumnName,
            IsSelected = IsFieldSelected(field.FieldName, visibility),
            DisplayOrder = index + 1
        }).ToList()
    };
    
    return viewConfig;
}

private bool IsFieldSelected(string fieldName, ColumnVisibilityState visibility)
{
    return fieldName switch
    {
        "SellerLastName" => visibility.SellerLastName,
        "SellerName" => visibility.SellerName,
        // ... all fields
        _ => false
    };
}
```

### Phase 6: ViewService Enhancements
#### 6.1 Add Method for Getting/Creating User View

Add to `ViewService.cs`:
```csharp
public async Task<(bool Success, ViewConfiguration? ViewConfig, string? Error)> 
    GetOrCreateUserViewAsync(string userId, string pageName, string module)
{
    // Check if user has existing view preference
    var userPref = await _repository.GetUserPagePreferenceAsync(
        int.Parse(userId), 
        pageName
    );
    
    if (userPref != null)
    {
        return (true, await GetViewConfigurationAsync(userPref.ViewID), null);
    }
    
    // Create default view for user
    var defaultView = await GetNewViewConfigurationAsync(module);
    defaultView.ViewName = $"User {userId} - {pageName}";
    
    var createResult = await CreateAsync(defaultView);
    if (!createResult.Success || createResult.View == null)
    {
        return (false, null, createResult.Error);
    }
    
    // Save preference
    await _repository.SaveUserPagePreferenceAsync(new UserPagePreference
    {
        UserID = int.Parse(userId),
        PageName = pageName,
        ViewID = createResult.View.ViewID
    });
    
    return (true, defaultView, null);
}
```

## Performance Considerations

### Caching Strategy
1. **Module-Level Views**: Cached at application startup, rarely changes
2. **DisplayFields**: Cached at application startup, static data
3. **User Preferences**: Cached on user login, invalidated on preference updates
4. **Cache Expiration**: None for Views/DisplayFields (assume restart on schema changes)
5. **User Cache Expiration**: 24 hours or on explicit save

### Memory Footprint
- **Views**: ~100 bytes × 50 views = ~5 KB
- **DisplayFields**: ~100 bytes × 200 fields = ~20 KB
- **User Preferences**: ~50 bytes × 1000 users × 5 pages = ~250 KB
- **Total**: ~275 KB (negligible)

### Database Impact
- **On Startup**: 2 queries (Views, DisplayFields)
- **On User Login**: 1 query (UserPagePreferences for that user)
- **On Column Change**: 2-3 queries (check existing view, update/create, update preference)
- **On Page Load**: 0 queries (all from cache)

## Testing Plan

### Unit Tests
1. ViewCacheService caching logic
2. ViewService CRUD operations
3. Mapping functions (ViewConfig ↔ ColumnVisibility)

### Integration Tests
1. Full flow: Save preferences → Reload page → Verify columns restored
2. Multi-user: Different users see different column sets
3. Cache invalidation: Update preference → Cache updates correctly
4. Startup: Application starts and warms cache successfully

### Manual Testing Checklist
- [ ] Initial page load shows default columns
- [ ] Change column visibility via dialog
- [ ] Click Apply - see Snackbar confirmation
- [ ] Refresh browser - columns persist
- [ ] Log out and back in - columns persist
- [ ] Different user sees default columns (not other user's settings)
- [ ] Application restart doesn't lose preferences

## Migration Path

### Step 1: Create Migration
```bash
dotnet ef migrations add AddLetterAgreementDisplayFields --project ../SSRBusiness.NET10
```

### Step 2: Update Database
```bash
dotnet ef database update --project ../SSRBusiness.NET10
```

### Step 3: Deploy Services
1. Deploy ViewCacheService
2. Deploy CacheWarmingService
3. Register in DependencyInjection

### Step 4: Update UI
1. Update LetterAgreementIndex.razor.cs
2. Test thoroughly
3. Deploy

### Step 5: Rollout to Other Pages
Once proven with LetterAgreement, apply same pattern to:
- Acquisitions
- Operators
- Documents
- Other index pages

## Potential Issues and Solutions

### Issue 1: Cache Invalidation on Cluster
**Problem**: Multiple web servers, cache updated on one server only
**Solution**: 
- Use distributed cache (Redis) instead of in-memory
- Or: Accept eventual consistency (refresh on next login)
- Or: Use SignalR to broadcast cache invalidation

### Issue 2: Schema Changes
**Problem**: Adding/removing columns requires code + DB changes
**Solution**: 
- DisplayFields are data-driven
- UI can dynamically render based on DisplayFields
- Requires more complex UI rendering logic

### Issue 3: Performance with Many Users
**Problem**: 10,000+ users could impact cache memory
**Solution**: 
- Only cache active users (last 24 hours)
- Use LRU eviction policy
- Move to Redis for better scalability

## Success Metrics
- Zero additional database queries on page load (cached)
- < 100ms to save column preference
- User preferences persist across sessions 100% of time
- Memory usage < 1 MB for cache
- No impact on page load performance

## Timeline Estimate
- Phase 1 (Database): 2 hours
- Phase 2 (Cache Service): 4 hours
- Phase 3 (Startup): 1 hour
- Phase 4 (Login): 1 hour
- Phase 5 (UI Updates): 4 hours
- Phase 6 (ViewService): 2 hours
- Testing: 4 hours
- **Total**: ~18 hours (~2.5 days)

## Dependencies
- Existing ViewService must remain functional
- Entity Framework migrations
- Authentication system must provide user ID
- No breaking changes to existing View usage (Operators, Documents pages)
