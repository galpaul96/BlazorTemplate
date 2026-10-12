using Blazor.ServiceDefaults;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System.Diagnostics;
using Blazor.Web.Components.Account;
using Blazor.Web.Data;
using Blazor.Web.Components.Admin;
using Blazor.Web.Services;
using Blazor.Web.Components;

namespace Blazor.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.ConfiugreServices(builder.Configuration);
        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents(options =>
            {
                options.DetailedErrors = builder.Environment.IsDevelopment();
            });

        builder.Services.Configure<CircuitOptions>(options =>
        {
            options.DetailedErrors = builder.Environment.IsDevelopment();
        });

        builder.AddServiceDefaults();
        builder.AddRedisOutputCache("redis-cache");

        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<IdentityUserAccessor>();
        builder.Services.AddScoped<IdentityRedirectManager>();
        builder.Services.AddScoped<IdentityAdminService>();
        builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();

        //var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        //builder.Services.AddDbContext<ApplicationDbContext>(options =>
        //options.UseSqlServer(connectionString));

        var connectionString = builder.Configuration.GetConnectionString("web-db") ?? throw new InvalidOperationException("Connection string 'web-db' not found.");

        static void ConfigureWebDbContext(DbContextOptionsBuilder options, string connectionString)
        {
            options.UseNpgsql(
                connectionString,
                x => x.MigrationsAssembly("Blazor.Web"));
        }

        builder.Services.AddDbContext<ApplicationDbContext>(
            options => ConfigureWebDbContext(options, connectionString));

        var sw = Stopwatch.StartNew();
        Console.WriteLine("Deploying database...");
        var dbContext = new EfContextFactory().CreateDbContext([connectionString]);
        dbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(30));
        dbContext.Database.Migrate();
        Console.WriteLine($"Deployment done in {sw.Elapsed}");

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
        }

        builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
        app.UseOutputCache();
        app.MapDefaultEndpoints();
        // Add additional endpoints required by the Identity /Account Razor components.
        app.MapAdditionalIdentityEndpoints();

        app.Run();
    }
}
