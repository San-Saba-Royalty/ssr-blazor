# MineralAcquisitionWeb - AI Agent Instructions

## Project Overview
This is **MineralAcquisitionWeb**, a .NET 10.0 Blazor SSR application for managing mineral rights acquisitions. It's a complete rewrite from a legacy ASP.NET Web Forms (VB.NET) application. The system manages the full lifecycle of mineral rights purchases for an energy company.

**Critical:** This project depends on `SSRBusiness.NET10` (referenced in `.csproj`), a separate business layer containing EF Core DbContext, repositories, and domain entities. Without it, the project won't compile.

## Architecture & Key Decisions

### Blazor Server-Side Rendering (SSR)
- Uses Interactive Server components (`@rendermode InteractiveServer` required for stateful components)
- NOT Blazor WebAssembly - all UI rendering happens server-side
- Real-time updates via SignalR connection
- **Critical**: Components with events/state management MUST have `@rendermode InteractiveServer`

### Dependency Injection Pattern
All service registration happens in [DependencyInjection.cs](DependencyInjection.cs):
- `AddApplication(IConfiguration)` extension method registers DbContext and repositories
- Repository pattern: `UserRepository`, `CountyRepository` injected via `IServiceCollection`
- Connection string: `"SanSabaConnection"` from appsettings.json

### Project References
```xml
<ProjectReference Include="..\SSRBusiness.NET10\SSRBusiness.csproj" />
```
This external project provides:
- `SsrDbContext` (Entity Framework Core)
- Business logic classes
- Repository implementations (`UserRepository`, `CountyRepository`)
- Domain entities

## MudBlazor UI Framework

### Core Dependencies
```xml
<PackageReference Include="MudBlazor" Version="8.15.0" />
<PackageReference Include="MudBlazor.Extensions" Version="8.15.1-prev-2512121815-main" />
<PackageReference Include="MudBlazor.Markdown" Version="8.11.0" />
<PackageReference Include="MudBlazor.ThemeManager" Version="3.0.0" />
<PackageReference Include="MudBlazorExt.RichTextEditor" Version="0.1.1" />
```

### Component Patterns (from existing code)
```csharp
// UserAdd.razor example
<MudContainer MaxWidth="MaxWidth.Medium" Class="mt-8">
    <MudCard Elevation="4">
        <MudCardContent>
            <MudTextField Label="Email" @bind-Value="User.Email" 
                          Variant="Variant.Outlined" Margin="Margin.Dense" />
        </MudCardContent>
        <MudCardActions>
            <MudButton ButtonType="ButtonType.Submit" Variant="Variant.Filled" 
                       Color="Color.Primary">Create</MudButton>
        </MudCardActions>
    </MudCard>
</MudContainer>
```

**Standard Components:**
- Data tables: `MudDataGrid`
- Forms: `MudForm` + `MudTextField` + validation
- Actions: `MudButton`, `MudIconButton`
- Dialogs: `MudDialog` for modals
- Feedback: `ISnackbar` for notifications
- Menus: `MudMenu` + `MudMenuItem` (must be inside `MudAppBar` for dropdowns to work)

### Critical MudBlazor Architecture
**Layout Structure** (from [MainLayout.razor](Components/Layout/MainLayout.razor)):
```csharp
<MudThemeProvider />
<MudPopoverProvider />      // Required for menus/dropdowns
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar>             // Menu goes HERE, not in MudMainContent
        <MainMenu />
    </MudAppBar>
    <MudMainContent>
        @Body               // Page content only
    </MudMainContent>
</MudLayout>
```
**Why this matters**: MudMenu dropdowns require proper positioning context from MudAppBar. Placing menus elsewhere breaks dropdown functionality.

## Navigation & Routing

### Route Structure
Routes defined in [Components/Routes.razor](Components/Routes.razor):
```csharp
<Router AppAssembly="typeof(Program).Assembly" 
        NotFoundPage="typeof(Pages.NotFound)">
```
All pages use `@page` directive at top of `.razor` files.

### Menu System
[Components/Navigation/MainMenu.razor.cs](Components/Navigation/MainMenu.razor.cs) implements command-based navigation:
- Menu commands use constants like `MNU_FILE_NEW_ACQUISITION`, `MNU_REPORT_DRAFTS_DUE`
- Navigation via `NavigationManager.NavigateTo("/acquisition/new")`
- EventCallback pattern for parent components: `OnMenuActionTriggered`, `OnFilterApplied`

**Menu Categories:**
1. File → New/Display (acquisitions, buyers, operators, counties, referrers)
2. Reports → Financial tracking (drafts, invoices, 1099s)
3. Documents → Template management
4. Tools → System configuration (statuses, lien types, curative types)
5. Administration → User/role management

## Component Organization

```
Components/
├── Layout/              # MainLayout.razor (application shell)
├── Navigation/          # MainMenu with code-behind
├── Pages/               # Routable pages
│   ├── Account/         # User management (UserAdd, UserEdit)
│   ├── Acquisition/     # Core feature (AcquisitionIndex, etc.)
│   └── [Other features]
├── App.razor            # Root component
└── Routes.razor         # Router configuration
```

### Code-Behind Pattern
Components can use `.razor.cs` code-behind (see [MainMenu.razor.cs](Components/Navigation/MainMenu.razor.cs)):
```csharp
public partial class MainMenu : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Parameter] public EventCallback<string> OnMenuActionTriggered { get; set; }
    // ...
}
```

## Business Domain

### Core Entities (from SSRBusiness.NET10)
- **Acquisition**: Main entity for mineral rights purchase
- **Buyer**: Client purchasing mineral rights
- **Seller**: Mineral rights owner
- **Referrer**: External contractor (commission-based, requires 1099 reporting)
- **Landman**: Internal staff (Office vs. Field roles)
- **Attorney**: Legal staff handling title/curative work
- **County/Operator**: Geographic and operational data

### Workflow States
- Acquisition statuses track deal progression
- Curative requirements for title defects
- Field check verification by field landmen
- Commission calculations (buyer pays company, optionally referrer fees)

### Permission Model
- System Administrator: Unrestricted access (flag in User entity)
- Role-based permissions control menu visibility
- Menu commands check permissions before display

## Development Workflows

### Build & Run
```bash
dotnet build                           # Compile project
dotnet run                             # Run with default profile (HTTP on port 5093)
dotnet run --launch-profile http       # HTTP on port 5093
dotnet run --launch-profile https      # HTTPS on port 7262
```

### Database (Entity Framework Core)
```bash
dotnet ef database update              # Apply migrations
dotnet ef migrations add [Name]        # Create new migration
```
**Note:** EF commands may require `--project SSRBusiness.NET10` flag if DbContext is in separate project.

### Docker
```bash
docker build -t ssrblazor .
docker-compose up --build
```
Ports: 8080 (HTTP), 8081 (HTTPS)

## Configuration Files

### [appsettings.json](appsettings.json)
```json
{
  "ConnectionStrings": {
    "SanSabaConnection": "Server=...;Database=SanSaba;..."
  }
}
```
**Security:** Production credentials should use environment variables or Azure Key Vault.

### [Program.cs](Program.cs) - Startup
```csharp
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddApplication(builder.Configuration);  // Custom DI
builder.Services.AddMudServices();                       // MudBlazor
app.UseStatusCodePagesWithReExecute("/not-found");      // Custom 404
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
```

### Project Settings (.csproj)
```xml
<BlazorDisableThrowNavigationException>true</BlazorDisableThrowNavigationException>
```
This setting prevents navigation exceptions during state changes.

## Migration Context

The [plans/](plans/) directory contains migration documentation from legacy ASP.NET Web Forms:
- Legacy system used VB.NET with master pages
- Permission system based on menu commands (e.g., `MNU_*` constants)
- **Document storage migration**: Legacy used DocuShare; being replaced with custom Azure Storage solution (in development)
- Extensive reporting (1099 forms, invoices, financial tracking)

When adding features, reference legacy patterns documented in [CLAUDE.md](CLAUDE.md) and [docs/README.md](docs/README.md) for domain context.

### Document Storage Strategy
- **Legacy**: DocuShare document management system
- **Target**: Custom Azure Storage solution (Azure Blob Storage)
- **Status**: Migration in progress - avoid hard-coding DocuShare references
- When implementing document features, design for Azure Blob Storage integration

## Common Patterns

### Creating New Pages
1. Add `.razor` file in `Components/Pages/[Feature]/`
2. Add `@page "/route"` directive
3. Inject services: `@inject UserRepository UserRepo` and `@inject ISnackbar Snackbar`
4. Use MudBlazor components for consistency
5. Add navigation entry in [MainMenu.razor.cs](Components/Navigation/MainMenu.razor.cs)

### Form Submission Pattern
```csharp
<EditForm Model="@entity" OnValidSubmit="HandleValidSubmit">
    <DataAnnotationsValidator />
    <MudButton ButtonType="ButtonType.Submit">Save</MudButton>
</EditForm>

@code {
    private async Task HandleValidSubmit() {
        await repository.SaveAsync(entity);
        Snackbar.Add("Saved successfully!", Severity.Success);
        Navigation.NavigateTo("/list");
    }
}
```

### Repository Pattern Usage
```csharp
@inject UserRepository UserRepo
@code {
    private List<User> users = new();
    protected override async Task OnInitializedAsync() {
        users = await UserRepo.GetAllAsync();
    }
}
```

## Testing & Debugging

### Launch Profiles (from [Properties/launchSettings.json](Properties/launchSettings.json))
- **http**: Port 5093 (default, launches browser)
- **https**: Port 7262
- Browser auto-launch enabled in development

### Common Issues
1. **Missing SSRBusiness reference**: Ensure sibling project exists at `../SSRBusiness.NET10/`
2. **Database connection**: Verify SQL Server accessible and connection string correct
3. **MudBlazor not rendering**: Check `builder.Services.AddMudServices()` is called
4. **Navigation exceptions**: `BlazorDisableThrowNavigationException` should be `true`

## AI Agent Guidelines

1. **Always use MudBlazor components** - Never use native HTML form elements
2. **Follow existing navigation patterns** - Add menu commands to MainMenu.razor.cs
3. **Inject repositories, not DbContext** - Use existing repository abstractions
4. **Use code-behind for complex logic** - Keep `.razor` files focused on markup
5. **Check business layer** - Many entities/logic live in SSRBusiness.NET10, not this project
6. **Preserve domain patterns** - Referrer commission logic, field check workflows are business-critical
7. **Security matters** - Respect role-based permissions, especially around financial data
8. **Snackbar for feedback** - Use `ISnackbar.Add()` for user notifications, not alerts

## Key Files to Reference

- [DependencyInjection.cs](DependencyInjection.cs) - Service registration
- [Components/Navigation/MainMenu.razor.cs](Components/Navigation/MainMenu.razor.cs) - Navigation commands
- [Components/Pages/Account/UserAdd.razor](Components/Pages/Account/UserAdd.razor) - Example MudBlazor form
- [CLAUDE.md](CLAUDE.md) - Detailed business domain documentation
- [docs/README.md](docs/README.md) - Legacy system persona analysis
