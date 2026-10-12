var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("redis-cache");

var mongo = builder.AddMongoDB("mongodb").AddDatabase("mongo-db");

var postgres = builder.AddPostgres("pgsql");
var apiDb = postgres.AddDatabase("api-db");
var webDb = postgres.AddDatabase("web-db");

var apiSharedSecret = builder.Configuration["ApiAuthentication:SharedSecret"];
if (string.IsNullOrWhiteSpace(apiSharedSecret))
{
    throw new InvalidOperationException("Environment variable 'ApiAuthentication__SharedSecret' is required by the AppHost.");
}

var apiService = builder.AddProject<Projects.Blazor_Api>("api")
    .WithEnvironment("ApiAuthentication__SharedSecret", apiSharedSecret)
    .WithHttpHealthCheck("/health")
    .WithReference(mongo)
    .WaitFor(mongo)
    .WithReference(apiDb)
    .WaitFor(apiDb);

builder.AddProject<Projects.Blazor_Web>("web")
    .WithEnvironment("ApiAuthentication__SharedSecret", apiSharedSecret)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithReference(webDb)
    .WaitFor(webDb);

builder.Build().Run();
