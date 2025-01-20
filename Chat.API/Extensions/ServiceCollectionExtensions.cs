using Chat.API.Authentication;
using Microsoft.AspNetCore.Authentication;

namespace Chat.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHeaderAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication("Header")
                .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>("Header", null);

            return services;
        }
    }
}
