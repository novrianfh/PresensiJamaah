using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PresensiJamaah.Infrastructure.Persistence;

namespace PresensiJamaah.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");
        var options = sp.GetRequiredService<IOptions<AdminSeedOptions>>().Value;

        if (options.ApplyMigrationsOnStartup)
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
        }

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync(Roles.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
        }

        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            logger.LogWarning("AdminSeed:Email/Password belum diisi; akun admin awal tidak dibuat.");
            return;
        }

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(options.Email) is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = options.Email,
            Email = options.Email,
            EmailConfirmed = true,
            NamaLengkap = options.NamaLengkap
        };

        var result = await userManager.CreateAsync(admin, options.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Gagal membuat akun admin awal: {errors}");
        }

        await userManager.AddToRoleAsync(admin, Roles.Admin);
        logger.LogInformation("Akun admin awal {Email} dibuat.", options.Email);
    }
}
