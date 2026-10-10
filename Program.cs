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
builder.Services.AddScoped<AcademicHolidayService>();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Auto-migrate and seed on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Sessions ADD COLUMN IsOnline INTEGER NOT NULL DEFAULT 0;"); } catch { }
    try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Sessions ADD COLUMN MeetingPlatform TEXT NULL;"); } catch { }
    try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Sessions ADD COLUMN MeetingLink TEXT NULL;"); } catch { }
    var holidaySvc = scope.ServiceProvider.GetRequiredService<AcademicHolidayService>();
    await holidaySvc.EnsureTableExistsAsync();
    await SeedData.InitializeAsync(db);

    var paymentSvc = scope.ServiceProvider.GetRequiredService<PaymentService>();
    var receiptsDir = Path.Combine(app.Environment.WebRootPath, "receipts");
    paymentSvc.ExportRm5050ReceiptsToDirectory(receiptsDir, app.Environment.WebRootPath);

    if (args.Contains("--export-rm-pdf"))
    {
        var overview = await paymentSvc.GetFacultyPaymentOverviewAsync(
            new DateTime(2026, 10, 1),
            new DateTime(2026, 10, 4),
            new DateTime(2026, 9, 4),
            new DateTime(2026, 10, 4));

        var allReceiptsPdf = paymentSvc.GenerateAllFacultyReceiptsPdf(
            overview,
            new DateTime(2026, 9, 4),
            new DateTime(2026, 10, 4),
            app.Environment.WebRootPath);
        File.WriteAllBytes(Path.Combine(receiptsDir, "All_8_Faculty_Receipts_04Sep_to_04Oct_2026.pdf"), allReceiptsPdf);

        var birajdarOverview = overview.FirstOrDefault(f => f.FacultyName.Contains("Birajdar", StringComparison.OrdinalIgnoreCase));
        if (birajdarOverview != null)
        {
            var birajdarPdf = paymentSvc.GenerateFacultyPaymentReceiptPdf(
                birajdarOverview,
                new DateTime(2026, 9, 4),
                new DateTime(2026, 10, 4),
                app.Environment.WebRootPath);
            File.WriteAllBytes(Path.Combine(receiptsDir, "Dr_Ganesh_Birajdar_AM_Receipt_04Sep_to_04Oct_2026.pdf"), birajdarPdf);
        }

        Console.WriteLine("=== FULL BILLING AUDIT (04 SEP 2026 - 04 OCT 2026) ===");
        foreach (var f in overview)
        {
            Console.WriteLine($"{f.FacultyName,-30} | Lec: {f.MonthlyLectureCount,5:0.#} | Prac: {f.MonthlyPracticalCount,5:0.#} | Payable: INR {f.MonthlyPayableAmount,8:N0}");
        }
        Console.WriteLine($"TOTAL                          | Lec: {overview.Sum(x => x.MonthlyLectureCount),5:0.#} | Prac: {overview.Sum(x => x.MonthlyPracticalCount),5:0.#} | Payable: INR {overview.Sum(x => x.MonthlyPayableAmount),8:N0}");
        return;
    }
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
