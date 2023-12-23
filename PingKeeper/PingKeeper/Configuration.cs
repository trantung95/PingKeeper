using Microsoft.Extensions.DependencyInjection;
using PingKeeper.Services;

namespace PingKeeper
{
    public static class Configuration
    {
        public static void AddPingKeeper(this IServiceCollection services)
        {
            services.AddScoped<IPingService, PingService>();
        }
    }
}
