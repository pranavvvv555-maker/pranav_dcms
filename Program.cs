using DCMSApp.Components;
using DCMSApp.Data;
using DCMSApp.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using QuestPDF.Infrastructure;
using System.Net.Sockets;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// This is a local, desktop-managed app. Avoid the Windows Event Log provider:
// standard user accounts cannot always write to it, and a harmless warning
// should never interrupt the application or show a debugger dialog.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var configuredUrls = builder.Configuration["urls"]
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
    ?? string.Empty;
var hasHttpsEndpoint = configuredUrls
    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Any(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

var envPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(envPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{envPort}");
}

// Store the Blazor data-protection keys with the application rather than in a user profile.
// This keeps the app reliable when it is run as a service or from a shared workstation.
var keyDirectory = new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys"));
if (!keyDirectory.Exists)
{
    keyDirectory.Create();
}
builder.Services.AddDataProtection().PersistKeysToFileSystem(keyDirectory);

// Database
var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=dcms.db";
var dbPathMatch = System.Text.RegularExpressions.Regex.Match(defaultConn, @"Data Source=(?<path>[^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
if (dbPathMatch.Success)
{
    var dbFilePath = dbPathMatch.Groups["path"].Value.Trim();
    var dbDir = Path.GetDirectoryName(dbFilePath);
    if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
    {
        Directory.CreateDirectory(dbDir);
    }
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(defaultConn);
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// MudBlazor
builder.Services.AddMudServices();

// Application Services
builder.Services.AddScoped<FacultyService>();
builder.Services.AddScoped<CourseService>();
builder.Services.AddScoped<TimetableService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<AssessmentService>();
builder.Services.AddScoped<MockInterviewService>();
builder.Services.AddScoped<MockInterviewImportService>();
builder.Services.AddScoped<StudentService>();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Auto-migrate and seed on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.InitializeAsync(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// HTTPS is handled by the configured HTTPS launch profile or by Cloudflare in
// the live setup. Do not attempt a local redirect when the app has only HTTP.
if (hasHttpsEndpoint)
    app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

try
{
    app.Run();
}
catch (Exception exception) when (ContainsAddressInUseException(exception))
{
    Console.Error.WriteLine("DCMS is already running at http://127.0.0.1:5025. Open that address in your browser instead of starting a second copy.");
}

static bool ContainsAddressInUseException(Exception exception) =>
    exception switch
    {
        SocketException socketException => socketException.SocketErrorCode == SocketError.AddressAlreadyInUse,
        AggregateException aggregateException => aggregateException.InnerExceptions.Any(ContainsAddressInUseException),
        _ when exception.InnerException is not null => ContainsAddressInUseException(exception.InnerException),
        _ => false
    };
