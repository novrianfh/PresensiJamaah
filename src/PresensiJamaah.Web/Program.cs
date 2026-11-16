using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using PresensiJamaah.Application;
using PresensiJamaah.Application.Common;
using PresensiJamaah.Infrastructure;
using PresensiJamaah.Infrastructure.Identity;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // Semua halaman wajib login, kecuali yang diberi [AllowAnonymous].
    builder.Services.AddControllersWithViews(options =>
    {
        var requireLogin = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        options.Filters.Add(new AuthorizeFilter(requireLogin));
    });
    builder.Services.AddApplication();
    builder.Services.AddSingleton(sp => new LocalClock(
        sp.GetRequiredService<TimeProvider>(),
        builder.Configuration["App:TimeZoneId"] ?? "Asia/Jakarta"));
    builder.Services.AddSingleton(new RekapOptions(
        builder.Configuration.GetValue("App:JamaahAktifMinimalPersen", RekapOptions.DefaultMinimalPersen)));
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    await IdentitySeeder.InitializeAsync(app.Services);

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Aplikasi berhenti karena kesalahan fatal");
}
finally
{
    Log.CloseAndFlush();
}
