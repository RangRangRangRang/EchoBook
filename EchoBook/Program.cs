using EchoBook.Data;
using EchoBook.Repositories;
using EchoBook.Repositories.Interfaces;
using EchoBook.Services;
using EchoBook.Services.Interfaces;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Free-tier PaaS hosts (Render, Railway, etc.) commonly inject the database location as a single
// DATABASE_URL in URI form (postgres://user:pass@host:port/db) rather than as a Npgsql keyword
// connection string. Prefer an explicit ConnectionStrings__DefaultConnection if one was set, but
// fall back to translating DATABASE_URL so a plain "connect the free Postgres add-on" deploy works
// with zero extra configuration.
var configuredConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (string.IsNullOrWhiteSpace(configuredConnectionString) && !string.IsNullOrWhiteSpace(databaseUrl))
{
    configuredConnectionString = ConvertDatabaseUrlToNpgsqlConnectionString(databaseUrl);
    builder.Configuration["ConnectionStrings:DefaultConnection"] = configuredConnectionString;
}

// Most free hosts assign the listening port at runtime via PORT and terminate TLS at their edge
// load balancer, forwarding plain HTTP to the container.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// MVC
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // The proxy in front of the container on these hosts isn't a fixed, known IP, so trust the
    // platform's edge network rather than restricting to a specific known proxy/network.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(configuredConnectionString));

// Repositories
builder.Services.AddScoped<IRecoveryKeyRepository, RecoveryKeyRepository>();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IReadingProgressRepository, ReadingProgressRepository>();
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
builder.Services.AddScoped<IBookmarkRepository, BookmarkRepository>();
builder.Services.AddScoped<IAudioCacheRepository, AudioCacheRepository>();

// Services
builder.Services.AddScoped<IRecoveryKeyService, RecoveryKeyService>();
builder.Services.AddScoped<ICurrentRecoveryKeyAccessor, CurrentRecoveryKeyAccessor>();
builder.Services.AddSingleton<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IEpubParsingService, EpubParsingService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IReaderService, ReaderService>();
builder.Services.AddScoped<ITextToSpeechClient, EdgeTtsClient>();
builder.Services.AddScoped<ISpeechService, SpeechService>();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/StatusCode/{0}");

// Skip the redirect when a platform's edge load balancer already terminates HTTPS and forwards
// plain HTTP internally (signalled by PORT being set) - UseForwardedHeaders above still lets
// UseHsts and any [RequireHttps] checks see the original scheme correctly.
if (string.IsNullOrWhiteSpace(port))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

// Serve extracted cover images (stored outside wwwroot, alongside uploaded epubs) at /library-assets/...
var fileStorage = app.Services.GetRequiredService<IFileStorageService>();
Directory.CreateDirectory(fileStorage.UploadsRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(fileStorage.UploadsRoot),
    RequestPath = "/library-assets"
});

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Apply pending EF Core migrations automatically on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await MigrateDatabaseAsync(db, logger);
}

app.Run();

// Applies pending migrations, tolerating one specific situation: the target database already
// has the app's tables (e.g. it was bootstrapped once from Scripts/001_initial_schema.sql, or
// from an older run/volume that predates this migration), but EF's own bookkeeping table
// (__EFMigrationsHistory) has no record of that - so a normal Migrate() tries to CREATE TABLE
// for something that already exists and fails with Postgres error 42P07 ("relation already
// exists"). When that specific error is hit, baseline the history table against the schema
// that's already there (mark every currently-defined migration as applied without re-running
// its SQL) instead of crashing, then retry - Migrate() will then see nothing pending.
static async Task MigrateDatabaseAsync(AppDbContext db, ILogger logger)
{
    try
    {
        await db.Database.MigrateAsync();
    }
    catch (PostgresException ex) when (ex.SqlState == "42P07")
    {
        logger.LogWarning(
            "Startup migration hit '{Message}' - the database already has tables that EF's " +
            "migration history doesn't know about. Baselining migration history against the " +
            "existing schema instead of recreating it.",
            ex.MessageText);

        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        using (var createHistoryCmd = connection.CreateCommand())
        {
            createHistoryCmd.CommandText = """
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" character varying(150) NOT NULL,
                    "ProductVersion" character varying(32) NOT NULL,
                    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
                );
                """;
            await createHistoryCmd.ExecuteNonQueryAsync();
        }

        foreach (var migrationId in db.Database.GetMigrations())
        {
            using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = """
                INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                VALUES (@id, @version)
                ON CONFLICT ("MigrationId") DO NOTHING;
                """;
            insertCmd.Parameters.Add(new NpgsqlParameter("id", migrationId));
            insertCmd.Parameters.Add(new NpgsqlParameter("version", "8.0.10"));
            await insertCmd.ExecuteNonQueryAsync();
        }

        logger.LogWarning(
            "Migration history baselined with {Count} migration(s) marked as already applied. " +
            "If your actual schema doesn't match what these migrations would have produced " +
            "(e.g. it's missing a column a later migration added), fix that manually - baselining " +
            "only stops EF from re-running SQL that already ran, it doesn't reconcile column-level " +
            "drift.",
            db.Database.GetMigrations().Count());

        // History now matches reality - this will see 0 pending migrations and return immediately.
        await db.Database.MigrateAsync();
    }
}

// Translates a postgres://user:password@host:port/database?sslmode=require URI - the format most
// free-tier hosts (Render, Railway, Supabase, Neon) hand out for their managed Postgres add-ons -
// into the keyword=value connection string Npgsql expects.
static string ConvertDatabaseUrlToNpgsqlConnectionString(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    var username = Uri.UnescapeDataString(userInfo[0]);
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
    var database = uri.AbsolutePath.TrimStart('/');

    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
    var sslMode = query["sslmode"] switch
    {
        "disable" => "Disable",
        "require" or "verify-ca" or "verify-full" => "Require",
        _ => "Prefer"
    };

    var connectionStringBuilder = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Username = username,
        Password = password,
        Database = database,
        SslMode = Enum.Parse<Npgsql.SslMode>(sslMode)
    };

    return connectionStringBuilder.ConnectionString;
}