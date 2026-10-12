using Blazor.Services;
using Blazor.ServiceDefaults;
using Blazor.Api.Security;
using Microsoft.AspNetCore.Authentication;

namespace Blazor.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        var sharedSecret = builder.Configuration[ApiKeyAuthenticationHandler.ConfigurationKey];
        if (string.IsNullOrWhiteSpace(sharedSecret))
        {
            throw new InvalidOperationException($"Configuration value '{ApiKeyAuthenticationHandler.ConfigurationKey}' is required.");
        }

        builder.Services
            .AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationHandler.SchemeName,
                _ => { });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(ApiKeyAuthenticationHandler.PolicyName, policy =>
                policy.AddAuthenticationSchemes(ApiKeyAuthenticationHandler.SchemeName)
                    .RequireAuthenticatedUser()
                    .RequireClaim("client_id", ApiKeyAuthenticationHandler.ClientId));
        });

        // Add services to the container.
        builder.Services.ConfigureServices(builder.Configuration);

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers().RequireAuthorization(ApiKeyAuthenticationHandler.PolicyName);

        app.Run();
    }
}
