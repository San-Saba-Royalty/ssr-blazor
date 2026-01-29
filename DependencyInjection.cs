using SSRBusiness.BusinessClasses;
using SSRBusiness.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SSRBlazor.Services;
using SSRBusiness.BusinessFramework;
using SSRBusiness.Entities;
using SSRBusiness.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using ReportService = SSRBusiness.BusinessClasses.ReportService;

namespace SSRBlazor;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Add memory cache
        services.AddMemoryCache();

        // Register PooledDbContextFactory for thread-safe DbContext pooling
        // This allows multiple contexts to be created from the pool as needed
        services.AddPooledDbContextFactory<SsrDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("SanSabaConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null)));

        // Register scoped SsrDbContext using the factory
        services.AddScoped<SsrDbContext>(provider =>
        {
            var factory = provider.GetRequiredService<IDbContextFactory<SsrDbContext>>();
            return factory.CreateDbContext();
        });

        // Register SsrDbContext as DbContext for Repository pattern
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<SsrDbContext>());

        // Register the repository pattern
        services.AddScoped(typeof(Repository<>));

        // Register specific repositories
        services.AddScoped<AcquisitionRepository>();
        services.AddScoped<AcquisitionDocumentRepository>();
        services.AddScoped<CountyRepository>();
        services.AddScoped<DisplayFieldRepository>();
        services.AddScoped<DocumentTemplateRepository>();
        services.AddScoped<LetterAgreementRepository>();
        services.AddScoped<ReferrerRepository>();
        services.AddScoped<OperatorRepository>();
        services.AddScoped<OperatorService>();
        services.AddScoped<ViewRepository>();
        services.AddScoped<ReportRepository>();
        services.AddScoped<ReportDraftsDueRepository>();
        services.AddScoped<RptBuyerInvoicesDueRepository>();
        services.AddScoped<RptCurativeRequirementsRepository>();
        services.AddScoped<RptLetterAgreementDealsRepository>();
        services.AddScoped<RptPurchasesRepository>();
        services.AddScoped<RptReferrer1099SummaryRepository>();

        services.AddScoped<BuyerRepository>();
        services.AddScoped<BuyerService>();

        services.AddScoped<StateRepository>();
        services.AddScoped<CountyRepository>();
        services.AddScoped<FilterRepository>();
        services.AddScoped<FilterFieldRepository>();

        // County/CAD Data Repositories
        services.AddScoped<CountyAppraisalGroupRepository>();
        services.AddScoped<CadTableRepository>();
        services.AddScoped<CadDataRepository>();
        services.AddScoped<CadDataService>();

        // Register System Lookup Repositories
        services.AddScoped<LienTypeRepository>();
        services.AddScoped<FolderLocationRepository>();
        services.AddScoped<DealStatusRepository>();
        services.AddScoped<CurativeTypeRepository>();
        services.AddScoped<LetterAgreementDealStatusRepository>();
        services.AddScoped<RoleRepository>();
        services.AddScoped<PermissionRepository>();

        // Register User and Referrer Form Repositories
        services.AddScoped<UserRepository>();
        services.AddScoped<ReferrerFormRepository>();

        // Register File Service
        // services.AddSingleton<IFileService, FileService>();

        // Register cached data services
        services.AddScoped<CachedDataService<User>>()
            .AddScoped<CachedDataService<Acquisition>>()
            .AddScoped<CachedDataService<AcquisitionDocument>>()
            .AddScoped<CachedDataService<Operator>>()
            .AddScoped<CachedDataService<County>>()
            .AddScoped<CachedDataService<View>>()
            .AddScoped<CachedDataService<Filter>>()
            .AddScoped<CachedDataService<FilterField>>()
            .AddScoped<CachedDataService<DocumentTemplate>>()
            .AddScoped<CachedDataService<State>>()
            .AddScoped<CachedDataService<County>>()
            .AddScoped<CachedDataService<AcquisitionCounty>>()
            .AddScoped<CachedDataService<AcquisitionLien>>()
            .AddScoped<CachedDataService<AcquisitionDocument>>()
            .AddScoped<CachedDataService<LetterAgreement>>()
            .AddScoped<CachedDataService<Buyer>>()
            .AddScoped<CachedDataService<Report>>()
            .AddScoped<CachedDataService<Referrer>>();

        // Register application services
        services.AddScoped<IActionService, ActionService>();
        services.AddScoped<AcquisitionService>();
        services.AddScoped<BuyerService>();
        services.AddScoped<BuyerContactService>();
        services.AddScoped<CountyService>();
        services.AddScoped<CountyContactService>();
        services.AddScoped<AcquisitionDocumentService>();
        services.AddScoped<DocumentService>();
        services.AddScoped<LetterAgreementService>();
        services.AddScoped<SSRBusiness.BusinessClasses.ReferrerService>();
        services.AddScoped<ReferrerUiService>();
        services.AddScoped<FilterService>();
        services.AddScoped<ViewService>();
        services.AddScoped<AppraisalGroupService>();
        services.AddScoped<ReportService>();
        services.AddScoped<SSRBlazor.Services.ReportService>();
        services.AddScoped<LookUpService>();
        services.AddScoped<ThemeService>();
        services.AddScoped<SessionStateService>();
        services.AddScoped<WordTemplateEngine>();
        services.AddScoped<LetterAgreementTemplateEngine>();
        services.AddScoped<DocumentComposer>();
        services.AddScoped<CoverSheetService>();
        services.AddScoped<IGeneratedDocumentService, GeneratedDocumentService>();
        services.AddScoped<IFileService, AzureFileShareFileService>();

        // Register ViewCacheService as singleton for application-wide caching
        services.AddSingleton<ViewCacheService>();

        // Register cache warming hosted service (runs on startup)
        services.AddHostedService<CacheWarmingService>();

        // Configure Data Protection to use local file system for development
        services.AddDataProtection();

        return services;
    }
}