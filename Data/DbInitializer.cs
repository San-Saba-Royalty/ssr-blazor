using Microsoft.EntityFrameworkCore;
using SSRBusiness.Data;
using SSRBusiness.Entities;
using SSRBusiness.Support;

namespace SSRBlazor.Data;

public static class DbInitializer
{
    public static async Task SeedAdministratorAccount(SsrDbContext context)
    {
        // Check if administrator account already exists
        var adminEmail = "tjames@prometheusags.ai";
        var existingAdmin = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        
if (existingAdmin != null)
        {
            Console.WriteLine($"Administrator account '{adminEmail}' already exists.");
            return;
        }

        // Generate salt and hash password using SaltedHash class
        var password = "P@n@m3r@!";
        var saltedHash = SaltedHash.Create(password, useSha1ForCompatibility: true);

        // Create administrator user
        var adminUser = new User
        {
            UserName = "tjames",
            FirstName = "Thomas",
            LastName = "James",
            Email = adminEmail,
            Password = saltedHash.Hash,
            Salt = saltedHash.Salt,
            IsActive = true,
            Administrator = true,
            Locked = false,
            NumberFailedAttempts = 0,
            DisplayToolbar = true
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        Console.WriteLine($"Administrator account '{adminEmail}' created successfully.");
    }
}
