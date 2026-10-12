using Blazor.Web.Gateway.ExternalApis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Blazor.Web.Gateway
{
    public static class Configurations
    {
        public static void ConfiugreGateway(this IServiceCollection services, IConfiguration configuration)
        {
            var sharedSecret = configuration["ApiAuthentication:SharedSecret"];
            if (string.IsNullOrWhiteSpace(sharedSecret))
            {
                throw new InvalidOperationException("Configuration value 'ApiAuthentication:SharedSecret' is required.");
            }

            services.TryAddScoped<IGatewayClient, GatewayClient>();
            services.AddHttpClient(GatewayClient.HttpClientName, client =>
            {
                client.DefaultRequestHeaders.Add("X-Api-Key", sharedSecret);
            })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
                .SetHandlerLifetime(TimeSpan.FromMinutes(30));
        }
    }
}
