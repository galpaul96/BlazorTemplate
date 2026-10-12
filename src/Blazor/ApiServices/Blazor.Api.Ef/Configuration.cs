using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics;

namespace Blazor.Api.Ef
{
    public static class Configuration
    {
        public static void ConfigureRepository(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.TryAddScoped<IRepository, Repository>();

            var connectionString = configuration.GetConnectionString("api-db")
                ?? throw new InvalidOperationException("Connection string 'api-db' not found.");

            services.AddDbContext<EfContext>(
            options =>
                options.UseNpgsql(
                    connectionString,
                   x => x.MigrationsAssembly("Blazor.Api.Ef"))
                );

            var sw = Stopwatch.StartNew();
            Console.WriteLine("Deploying database...");
            var dbContext = new EfContextFactory().CreateDbContext([connectionString]);
            dbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(30));
            dbContext.Database.Migrate();
            Console.WriteLine($"Deployment done in {sw.Elapsed}");
        }
    }
}
