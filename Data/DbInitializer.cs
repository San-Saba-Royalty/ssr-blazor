using Microsoft.EntityFrameworkCore;
using SSRBusiness.Data;
using SSRBusiness.Entities;
using System.Security.Cryptography;
using System.Text;

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

        // Generate salt and hash password
        var password = "P@n@m3r@!";
        var salt = GenerateSalt();
        var hashedPassword = HashPassword(password, salt);

        // Create administrator user
        var adminUser = new User
        {
            UserName = "tjames",
            FirstName = "Thomas",
            LastName = "James",
            Email = adminEmail,
            Password = hashedPassword,
            Salt = salt,
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

    private static string GenerateSalt()
    {
        var saltBytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        return Convert.ToBase64String(saltBytes);
    }

    private static string HashPassword(string password, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        
        // Combine password and salt
        var combined = new byte[passwordBytes.Length + saltBytes.Length];
        Buffer.BlockCopy(passwordBytes, 0, combined, 0, passwordBytes.Length);
        Buffer.BlockCopy(saltBytes, 0, combined, passwordBytes.Length, saltBytes.Length);

        // Hash using SHA256 (matching legacy implementation)
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(combined);
            return Convert.ToBase64String(hash);
        }
    }
}
