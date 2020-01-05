using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using App.Metrics;
using App.Metrics.Counter;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;


namespace IB.WatchServer.Service.Infrastructure
{
    /// <summary>
    /// Authentication request handler
    /// Looking for a known token to authenticate request, no token is Ok, wrong token - frobidden
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

        /// <summary>
        /// Looking for a known token to authenticate request, no token is Ok, wrong token - frobidden
        /// </summary>
        /// <returns></returns>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var path = Context.GetMetricsCurrentRouteName();//Request.Path.Value;
            if (!Request.Query.ContainsKey(Options.ApiTokenName))
            {
                _metrics.Measure.Counter.Increment(new CounterOptions{Name = "token_no_token", MeasurementUnit = Unit.Calls}, path);
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (Options.ApiToken != Request.Query[Options.ApiTokenName])
            {
                _metrics.Measure.Counter.Increment(new CounterOptions { Name = "token_wrong_token", MeasurementUnit = Unit.Calls }, path);
                return Task.FromResult(AuthenticateResult.Fail("Invalid auth token."));
            }

            // Create authenticated user
            //
            var identities = new List<ClaimsIdentity> { new GenericIdentity("watch-face"), new ClaimsIdentity(Options.Scheme) };
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identities), Options.Scheme);

            _metrics.Measure.Counter.Increment(new CounterOptions { Name = "token_ok_token", MeasurementUnit = Unit.Calls }, path);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            _metrics.Measure.Counter.Increment(
                new CounterOptions{Name = "token_forbidden", MeasurementUnit = Unit.Calls}, 
                Context.GetMetricsCurrentRouteName());
            Logger.LogInformation("Token forbidden, agent {agent}, request {request}", 
                Request.Headers[HeaderNames.UserAgent], Request.QueryString);
            
            await ForbidAsync(new AuthenticationProperties());
            await Response.WriteAsync(JsonSerializer.Serialize(new ErrorDescription(403, "Unauthorized access")));
        }
    }

    internal class ErrorDescription
    {
        public ErrorDescription(int code, string description)
        {
            Code = code;
            Description = description;
        }

        public int Code { get; set; }
        public string Description { get; set; }
        
    }
}