using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PresensiJamaah.Application.Abstractions;
using PresensiJamaah.Infrastructure.Excel;
using PresensiJamaah.Infrastructure.Identity;
using PresensiJamaah.Infrastructure.Persistence;
using PresensiJamaah.Infrastructure.Qr;

namespace PresensiJamaah.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default belum dikonfigurasi.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.User.RequireUniqueEmail = true;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<AppDbContext>();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.Cookie.HttpOnly = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(12);
            options.SlidingExpiration = true;
        });

        services.AddScoped<IJamaahRepository, JamaahRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IPresensiRepository, PresensiRepository>();
        services.AddSingleton<IJamaahExcel, JamaahExcel>();
        services.AddSingleton<IQrCodeGenerator, QrCodeGenerator>();
        services.AddSingleton<IRekapExcel, RekapExcel>();

        services.Configure<AdminSeedOptions>(configuration.GetSection(AdminSeedOptions.SectionName));

        return services;
    }
}
