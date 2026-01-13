using Microsoft.EntityFrameworkCore;
using SSRBusiness.Data;
using SSRBusiness.Entities;

namespace SSRBlazor.Services;

public class BuyerContactService(IDbContextFactory<SsrDbContext> dbFactory)
{
    public async Task<List<BuyerContact>> GetByBuyerIdAsync(int buyerId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        return await context.BuyerContacts
            .Where(c => c.BuyerID == buyerId)
            .OrderBy(c => c.ContactName)
            .ToListAsync();
    }

    public async Task<BuyerContact?> GetByIdAsync(int contactId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        return await context.BuyerContacts
            .Include(c => c.Buyer)
            .FirstOrDefaultAsync(c => c.BuyerContactID == contactId);
    }

    public async Task AddAsync(BuyerContact contact)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        context.BuyerContacts.Add(contact);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(BuyerContact contact)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        context.Entry(contact).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int contactId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var contact = await context.BuyerContacts.FindAsync(contactId);
        if (contact != null)
        {
            context.BuyerContacts.Remove(contact);
            await context.SaveChangesAsync();
        }
    }
}
