using SSRBusiness.BusinessClasses;
using SSRBusiness.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SSRBlazor.Services;
using SSRBusiness.BusinessFramework;
using SSRBusiness.Entities;
using SSRBusiness.Interfaces;
using ReportService = SSRBusiness.BusinessClasses.ReportService;

namespace SSRBlazor;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Add memory cache
        services.AddMemoryCache();

        services.AddDbContext<SsrDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("SanSabaConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null)));

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

        // Register System Lookup Repositories
        services.AddScoped<LienTypeRepository>();
        services.AddScoped<FolderLocationRepository>();
        services.AddScoped<DealStatusRepository>();
        services.AddScoped<CurativeTypeRepository>();
        services.AddScoped<LetterAgreementDealStatusRepository>();
        services.AddScoped<RoleRepository>();
        services.AddScoped<PermissionRepository>();

        // Register File Service
        services.AddSingleton<IFileService, FileService>();

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

        return services;
    }
}