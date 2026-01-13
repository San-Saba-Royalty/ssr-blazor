# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an SSR (Server-Side Rendering) Blazor application called **MineralAcquisitionWeb** - a comprehensive mineral rights acquisition management system ported from a legacy ASP.NET Web Forms application. The system manages the complete lifecycle of mineral rights purchases for an energy company (San Saba Resources/SSR).

**Framework:** .NET 10.0 with Blazor Server-Side Rendering
**UI Library:** MudBlazor (Material Design components)
**Database:** SQL Server with Entity Framework Core
**Architecture:** Clean Architecture with dependency injection

## Development Commands

### Building and Running
```bash
# Build the application
dotnet build

# Run in development mode (launches browser automatically)
dotnet run

# Run with specific profile
dotnet run --launch-profile https    # HTTPS on port 7262
dotnet run --launch-profile http     # HTTP on port 5093

# Build for release
dotnet build -c Release

# Publish the application
dotnet publish -c Release
```

### Database Operations
```bash
# Update database (EF migrations)
dotnet ef database update

# Add new migration
dotnet ef migrations add [MigrationName]

# Generate EF scaffolding (if needed)
dotnet ef dbcontext scaffold "connection_string" Microsoft.EntityFrameworkCore.SqlServer
```

### Docker Operations
```bash
# Build Docker image
docker build -t ssrblazor .

# Run with Docker Compose
docker-compose up

# Build and run with Docker Compose
docker-compose up --build
```

## Architecture and Code Organization

### Project Structure
```
/Components
  /Layout          - Main application layout components
  /Pages           - Razor page components organized by feature
    /Account       - User management (UserAdd, UserEdit)
    /Acquisition   - Core acquisition management
  /Navigation      - Navigation-related components
/Models           - View models and data transfer objects
/Properties       - Launch settings and configuration
/wwwroot          - Static assets (CSS, JS, images)
```

### Key Architectural Patterns

1. **Server-Side Rendering (SSR)**: Uses Blazor's Interactive Server Components for real-time updates
2. **Dependency Injection**: Centralized service registration in `DependencyInjection.cs`
3. **Repository Pattern**: Data access abstracted through repositories (`UserRepository`, `CountyRepository`)
4. **Clean Architecture**: Separation between UI, business logic, and data access layers

### External Dependencies and Business Logic

**Important**: This project references `SSRBusiness.NET10` - a separate business layer project that contains:
- Entity Framework DbContext (`SsrDbContext`)
- Business logic classes
- Repository implementations
- Domain entities

The business layer is **not included in this repository** but is essential for the application to function.

### Database Configuration

Connection string is configured in `appsettings.json` as `"SanSabaConnection"` and uses SQL Server. The application expects:
- SQL Server database with proper schema
- Entity Framework migrations applied
- Reference data populated (users, roles, permissions, etc.)

### Component Patterns

#### Page Components
- Located in `/Components/Pages/`
- Follow naming convention: `[Feature][Action].razor` (e.g., `UserAdd.razor`, `UserEdit.razor`)
- Use code-behind files when complex logic is needed

#### Layout System
- `MainLayout.razor` - Primary application shell
- `MainLayout.razor.css` - Layout-specific styling
- Navigation managed through dedicated navigation components

### MudBlazor Integration

The application heavily uses MudBlazor components:
- `MudBlazor` - Core components
- `MudBlazor.Extensions` - Extended functionality
- `MudBlazor.Markdown` - Markdown rendering
- `MudBlazor.ThemeManager` - Theme management
- `MudBlazorExt.RichTextEditor` - Rich text editing

Key MudBlazor patterns:
- Use `MudDataGrid` for data tables
- `MudForm` with `MudTextField` for forms
- `MudButton`, `MudIconButton` for actions
- `MudDialog` for modal interactions

## Configuration Notes

### Launch Profiles
- **HTTPS Profile**: `https://localhost:7262` (primary development)
- **HTTP Profile**: `http://localhost:5093` (fallback)
- Browser automatically launches in development mode

### Blazor Configuration
- `BlazorDisableThrowNavigationException` is enabled in project settings
- Interactive Server Components are configured for real-time updates
- Antiforgery protection is enabled

### Docker Configuration
- Multi-stage Dockerfile optimized for .NET 10.0
- Exposes ports 8080 (HTTP) and 8081 (HTTPS)
- Uses non-root user for security
- Includes build optimization for production

## Key Business Domain Concepts

Understanding the domain is crucial for effective development:

### Core Entities
- **Acquisition**: Main entity representing a mineral rights purchase
- **Buyer**: External clients purchasing mineral rights
- **Seller**: Mineral rights owners selling their rights
- **Referrer**: External contractors who bring deals (commission-based)
- **Landman**: Internal staff (office and field) managing acquisitions
- **Attorney**: Legal staff handling title issues and curative work

### Workflow States
The application manages complex workflows with various status tracking for acquisitions, legal reviews, field checks, and financial processing.

### Permission System
Role-based access control with granular permissions. The system has special handling for:
- System Administrator (unrestricted access)
- Office Landman (full operational access)
- Field Landman (limited field-specific functions)
- External users (restricted access)

## Development Guidelines

### Adding New Features
1. Create appropriate models in `/Models/` if needed
2. Add repository methods in the business layer
3. Create Razor components in `/Components/Pages/[Feature]/`
4. Register any new services in `DependencyInjection.cs`
5. Use MudBlazor components for UI consistency

### Database Changes
1. Modify entities in the business layer project
2. Add EF migration: `dotnet ef migrations add [Name]`
3. Update database: `dotnet ef database update`
4. Update repository methods as needed

### Component Development
- Follow Blazor Server patterns (not WASM)
- Use `@rendermode InteractiveServer` for real-time updates
- Implement proper error handling and validation
- Follow MudBlazor design patterns for consistency

### Security Considerations
- All pages should implement proper authorization
- Use role-based access control patterns from existing pages
- Validate user permissions before allowing actions
- Secure sensitive data (financial information, personal data)

## Common Issues and Solutions

### Missing Business Layer
If you encounter compilation errors related to `SSRBusiness`, the separate business layer project needs to be available and properly referenced.

### Database Connection Issues
Ensure the SQL Server connection string in `appsettings.json` is correct and the database is accessible with proper migrations applied.

### MudBlazor Styling Issues
Ensure MudBlazor services are properly registered and themes are configured. Check that CSS imports are correct in the layout files.