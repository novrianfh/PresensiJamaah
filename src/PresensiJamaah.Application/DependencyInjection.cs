using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PresensiJamaah.Application.Services;

namespace PresensiJamaah.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<JamaahService>();
        services.AddScoped<JamaahImportService>();
        services.AddScoped<EventService>();
        services.AddScoped<PresensiService>();
        services.AddScoped<RekapService>();
        // RekapOptions didaftarkan di Program.cs (dari konfigurasi App:JamaahAktifMinimalPersen).
        return services;
    }
}
