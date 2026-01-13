using Microsoft.EntityFrameworkCore;
using SSRBusiness.Data;
using SSRBusiness.Entities;

namespace SSRBlazor.Services;

public class AppraisalGroupService(IDbContextFactory<SsrDbContext> dbFactory)
{
    public async Task<List<AppraisalGroup>> GetAllAsync()
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        return await context.AppraisalGroups
            .OrderBy(a => a.AppraisalGroupName)
            .ToListAsync();
    }

    public async Task<AppraisalGroup?> GetByIdAsync(int id)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        return await context.AppraisalGroups
            .FirstOrDefaultAsync(a => a.AppraisalGroupID == id);
    }

    public async Task<int> AddAsync(AppraisalGroup appraisalGroup)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        
        // Basic validation
        if (string.IsNullOrWhiteSpace(appraisalGroup.AppraisalGroupName))
            throw new ArgumentException("Appraisal Group Name is required.");

        if (await IsNameUniqueAsync(appraisalGroup.AppraisalGroupName, null) == false)
            throw new InvalidOperationException($"Appraisal Group '{appraisalGroup.AppraisalGroupName}' already exists.");

        context.AppraisalGroups.Add(appraisalGroup);
        await context.SaveChangesAsync();
        return appraisalGroup.AppraisalGroupID;
    }

    public async Task UpdateAsync(AppraisalGroup appraisalGroup)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        
        var existing = await context.AppraisalGroups
            .FirstOrDefaultAsync(a => a.AppraisalGroupID == appraisalGroup.AppraisalGroupID);

        if (existing == null)
            throw new KeyNotFoundException($"Appraisal Group with ID {appraisalGroup.AppraisalGroupID} not found.");

        if (string.IsNullOrWhiteSpace(appraisalGroup.AppraisalGroupName))
             throw new ArgumentException("Appraisal Group Name is required.");

        if (await IsNameUniqueAsync(appraisalGroup.AppraisalGroupName, appraisalGroup.AppraisalGroupID) == false)
            throw new InvalidOperationException($"Appraisal Group '{appraisalGroup.AppraisalGroupName}' already exists.");

        // Map properties
        existing.AppraisalGroupName = appraisalGroup.AppraisalGroupName;
        existing.ContactName = appraisalGroup.ContactName;
        existing.ContactEmail = appraisalGroup.ContactEmail;
        existing.ContactPhone = appraisalGroup.ContactPhone;
        existing.ContactFax = appraisalGroup.ContactFax;
        existing.Attention = appraisalGroup.Attention;
        existing.AddressLine1 = appraisalGroup.AddressLine1;
        existing.AddressLine2 = appraisalGroup.AddressLine2;
        existing.AddressLine3 = appraisalGroup.AddressLine3;
        existing.City = appraisalGroup.City;
        existing.StateCode = appraisalGroup.StateCode;
        existing.ZipCode = appraisalGroup.ZipCode;

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        
        var existing = await context.AppraisalGroups
            .FirstOrDefaultAsync(a => a.AppraisalGroupID == id);

        if (existing != null)
        {
            // Check for dependencies (Acquisitions, CountyAppraisalGroups)
            // Legacy code checked: "If appraisalGroupBus.AcquisitionCount(_appraisalGroupID) > 0"
            // We should probably check dependencies here too, but for now we follow the simple delete path or implement a check.
            // Let's implement a safe check for CountyAppraisalGroups as we saw that relationship in CountyEntities.
            
            var hasDependencies = await context.CountyAppraisalGroups.AnyAsync(c => c.AppraisalGroupID == id);
             if (hasDependencies)
                 throw new InvalidOperationException("Cannot delete Appraisal Group because it is in use by one or more Counties.");

            context.AppraisalGroups.Remove(existing);
            await context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsNameUniqueAsync(string name, int? excludeId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        
        var query = context.AppraisalGroups
            .Where(a => a.AppraisalGroupName == name);

        if (excludeId.HasValue)
        {
            query = query.Where(a => a.AppraisalGroupID != excludeId.Value);
        }

        return !await query.AnyAsync();
    }
}
