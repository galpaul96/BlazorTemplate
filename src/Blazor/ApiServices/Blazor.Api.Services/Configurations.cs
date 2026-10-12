using AutoMapper;
using Blazor.Api.Ef;
using Blazor.MongoDb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Blazor.Services
{
    public static class Configurations
    {
        public static void ConfigureServices(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.ConfigureRepository(configuration);
            services.ConfigureMongoDb(configuration);

            services.AddAutoMapper(x =>
            {
                x.AddProfile(new ServiceMapperProfile());
            });

            //services.AddHttpClient("foo"); // adding an HttpClient named "foo" with a default configuration

            //services.AddHttpClient("api", c => c.BaseAddress = new Uri("https://www.example.com")) // configuring HttpClient itself
            //    //.AddHttpMessageHandler<MyAuthHandler>() // adding additional delegating handlers to form a message handler chain
            //    .ConfigurePrimaryHttpMessageHandler(b => new HttpClientHandler() { AllowAutoRedirect = false }) // configuring primary handler
            //    .SetHandlerLifetime(TimeSpan.FromMinutes(30)); // changing the handler recycling interval
        }
    }
}
