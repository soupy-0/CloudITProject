using Microsoft.EntityFrameworkCore;
using MeetSync.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(connectionString);
    
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    _ = Task.Run(async () =>
    {
        await Task.Delay(5000);
        
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        try
        {
            logger.LogInformation("🔄 Starting background database initialization...");
            
            var canConnect = await context.Database.CanConnectAsync();
            if (canConnect)
            {
                logger.LogInformation("✅ Database connection successful");
                
              
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation("Applying {Count} pending migrations: {Migrations}", 
                        pendingMigrations.Count(), string.Join(", ", pendingMigrations));
                    
                    await context.Database.MigrateAsync();
                    logger.LogInformation("✅ Database migrations completed");
                }
                else
                {
                    logger.LogInformation("✅ No pending migrations");
                }
                
                
                var userCount = await context.Users.CountAsync();
                logger.LogInformation("✅ Database verification: {UserCount} users", userCount);
            }
            else
            {
                logger.LogError("❌ Cannot connect to database");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Background database initialization failed: {Message}", ex.Message);
        }
    });
}
else
{
    
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Development: Testing database connection...");
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var canConnect = await context.Database.CanConnectAsync(cts.Token);
        
        if (canConnect)
        {
            logger.LogInformation("✅ Database connected in development");
            await context.Database.MigrateAsync(cts.Token);
        }
    }
    catch (OperationCanceledException)
    {
        logger.LogWarning("⚠️ Database operation timed out - continuing startup");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "⚠️ Database connection failed - continuing startup");
    }
}


app.MapGet("/health", () => Results.Ok(new { 
    Status = "Healthy", 
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName 
}));


app.MapGet("/health/database", async (ApplicationDbContext context) =>
{
    try
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var canConnect = await context.Database.CanConnectAsync(cts.Token);
        
        if (canConnect)
        {
            var userCount = await context.Users.CountAsync(cts.Token);
            return Results.Ok(new { 
                Status = "Healthy", 
                DatabaseConnected = true,
                UserCount = userCount,
                Timestamp = DateTime.UtcNow 
            });
        }
        
        return Results.Problem("Database connection failed");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Database health check failed: {ex.Message}");
    }
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();