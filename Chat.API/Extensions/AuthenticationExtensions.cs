using Chat.API.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;

namespace Chat.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = jwtSettings?.Authority;
            options.Audience = jwtSettings?.Audience;
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false, // true on prod
                ValidateAudience = false, // true on prod
                ValidateLifetime = false, // true on prod
                ValidateIssuerSigningKey = false, // true on prod
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;
                    if (claimsIdentity == null)
                        return Task.CompletedTask;

                    // Get the raw JSON value of realm_access
                    var realmAccessClaim = context.Principal.Claims
                        .FirstOrDefault(c => c.Type == "realm_access")?.Value;

                    if (!string.IsNullOrEmpty(realmAccessClaim))
                    {
                        try
                        {
                            // Parse the JSON string
                            using var doc = JsonDocument.Parse(realmAccessClaim);
                            var rolesElement = doc.RootElement.GetProperty("roles");

                            // Add each role as a claim
                            foreach (var role in rolesElement.EnumerateArray())
                            {
                                var roleValue = role.GetString();
                                if (!string.IsNullOrEmpty(roleValue))
                                {
                                    claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            // Log the error but don't throw
                            Console.WriteLine($"Error parsing realm_access: {ex.Message}");
                        }
                    }

                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }
}