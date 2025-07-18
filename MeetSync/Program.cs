using Microsoft.EntityFrameworkCore;
using MeetSync.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();

// Add Entity Framework (add this back once ApplicationDbContext exists)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Starting database migration with Azure AD authentication...");
        
        // Test connection with retry logic
        var retryCount = 0;
        var maxRetries = 3;
        bool connected = false;
        
        while (!connected && retryCount < maxRetries)
        {
            try
            {
                connected = await context.Database.CanConnectAsync();
                if (connected)
                {
                    logger.LogInformation("Successfully connected to Azure SQL Database using Azure AD");
                    break;
                }
            }
            catch (Exception ex)
            {
                retryCount++;
                logger.LogWarning(ex, "Connection attempt {RetryCount} failed, retrying...", retryCount);
                if (retryCount < maxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5)); // Wait before retry
                }
            }
        }
        
        if (connected)
        {
            // Apply migrations
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            logger.LogInformation($"Pending migrations: {string.Join(", ", pendingMigrations)}");
            
            if (pendingMigrations.Any())
            {
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migration completed successfully");
            }
            else
            {
                logger.LogInformation("No pending migrations found");
            }
            
            // Verify tables exist
            var userCount = await context.Users.CountAsync();
            logger.LogInformation($"Current user count in database: {userCount}");
        }
        else
        {
            logger.LogError("Failed to connect to database after {MaxRetries} attempts", maxRetries);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed: {Message}", ex.Message);
        logger.LogError("Inner exception: {InnerException}", ex.InnerException?.Message);
        
        // In production, don't crash the app - let it start without migration
        if (app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();