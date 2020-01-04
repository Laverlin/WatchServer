using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using App.Metrics;


namespace IB.WatchServer.Service.Infrastructure
{
    /// <summary>
    /// Authentication request handler
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TokenAuthenticationHandler : AuthenticationHandler<TokenAuthOptions>
    {
        private readonly IMetrics _metrics;

        public TokenAuthenticationHandler(
            IOptionsMonitor<TokenAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IMetrics metrics)
            : base(options, logger, encoder, clock)
        {
            _metrics = metrics;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Query.ContainsKey(Options.ApiTokenName))
                return Task.FromResult(AuthenticateResult.NoResult());

            if (Options.ApiToken != Request.Query[Options.ApiTokenName])
                return Task.FromResult(AuthenticateResult.Fail("Invalid auth token."));

            // Create authenticated user
            //
            var identities = new List<ClaimsIdentity> { new GenericIdentity("watch-face"), new ClaimsIdentity(Options.Scheme) };
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identities), Options.Scheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            await ForbidAsync(new AuthenticationProperties());
        }
    }
}