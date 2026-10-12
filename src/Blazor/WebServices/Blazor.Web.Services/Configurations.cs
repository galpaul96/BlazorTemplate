using Blazor.Web.Gateway;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Blazor.Web.Services
{
    public static class Configurations
    {
        public static void ConfiugreServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.ConfiugreGateway(configuration);

            //services.TryAddScoped<IModulesService, ModulesService>();
        }
    }
}
