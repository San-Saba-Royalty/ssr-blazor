using Microsoft.EntityFrameworkCore;
using SSRBusiness.Data;
using SSRBusiness.Entities;

namespace SSRBlazor.Services;

public class CountyContactService(IDbContextFactory<SsrDbContext> dbFactory)
{
    public async Task<List<CountyContact>> GetByCountyIdAsync(int countyId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        return await context.CountyContacts
            .Where(c => c.CountyID == countyId)
            .OrderBy(c => c.ContactName)
            .ToListAsync();
    }

    public async Task<CountyContact?> GetByIdAsync(int contactId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        return await context.CountyContacts
            .Include(c => c.County)
            .FirstOrDefaultAsync(c => c.CountyContactID == contactId);
    }

    public async Task AddAsync(CountyContact contact)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        context.CountyContacts.Add(contact);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CountyContact contact)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        context.Entry(contact).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int contactId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var contact = await context.CountyContacts.FindAsync(contactId);
        if (contact != null)
        {
            context.CountyContacts.Remove(contact);
            await context.SaveChangesAsync();
        }
    }
}
