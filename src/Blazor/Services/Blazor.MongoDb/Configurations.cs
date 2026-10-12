using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Blazor.MongoDb.Repository;

namespace Blazor.MongoDb;

public static class Configurations
{
    public static void ConfigureMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("mongo-db")
            ?? throw new InvalidOperationException("Connection string 'mongo-db' not found.");

        services.TryAddSingleton<IMongoClient>(_ => new MongoClient(connectionString));

        services.TryAddSingleton(serviceProvider =>
        {
            var client = serviceProvider.GetRequiredService<IMongoClient>();
            return client.GetDatabase("mongo-db");
        });

        services.TryAddScoped(typeof(IRepository<>), typeof(Repository<>));
    }
}
