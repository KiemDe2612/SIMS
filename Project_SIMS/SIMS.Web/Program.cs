using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;
using SIMS.Persistence.Data;
using SIMS.Persistence.Repositories;
using SIMS.Application.Interfaces;
using SIMS.Application.Services;
using SIMS.Application.Services.Observers;
using SIMS.Application.Services.Strategies;
using SIMS.Infrastructure.Repositories;
using SIMS.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity
builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();

// Memory Cache
builder.Services.AddMemoryCache();

// Add DI for Repositories and Unit of Work
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<GenericRepository<Student>>();
builder.Services.AddScoped<IGenericRepository<Student>>(sp =>
{
    var inner = sp.GetRequiredService<GenericRepository<Student>>();
    var cache = sp.GetRequiredService<IMemoryCache>();
    return new CachingStudentRepository(inner, cache);
});
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Add DI for Services
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IFacultyService, FacultyService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IStudentPortalService, StudentPortalService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IGpaCalculationStrategy, TenPointStrategy>();
builder.Services.AddScoped<ITranscriptService, TranscriptService>();
builder.Services.AddScoped<IUserFactory, UserFactory>();

// Add Observer Pattern for Enrollments
builder.Services.AddScoped<IEnrollmentObserver, AuditLogEnrollmentObserver>();
builder.Services.AddScoped<IEnrollmentObserver, NotificationEnrollmentObserver>();
builder.Services.AddScoped<IEnrollmentSubject, EnrollmentSubject>();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.SeedUsersAndRolesAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
