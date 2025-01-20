using Microsoft.AspNetCore.Authentication;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.Schemas;
using Microsoft.Extensions.Options;
using Polly;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Chat.API.Authentication
{
    public class HeaderAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public HeaderAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var userId = Context.Request.Headers["X-User-Id"].ToString();
            var roles = Context.Request.Headers["X-User-Roles"].ToString().Split(',');

            if (string.IsNullOrEmpty(userId))
            {
                return Task.FromResult(AuthenticateResult.Fail("User ID not found in headers"));
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
        };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
