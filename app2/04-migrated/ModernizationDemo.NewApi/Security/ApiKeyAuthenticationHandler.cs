using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ModernizationDemo.NewCore;

namespace ModernizationDemo.NewApi.Security;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
	private readonly ShopEntities shopEntities;

	public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, ShopEntities shopEntities) : base(options, logger, encoder, clock)
	{
		this.shopEntities = shopEntities;
	}

	public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ShopEntities shopEntities) : base(options, logger, encoder)
	{
		this.shopEntities = shopEntities;
	}

	protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		if (Request.Headers.TryGetValue("X-ApiKey", out var apiKeyValues))
		{
			var apiKey = apiKeyValues.First();
			
			var now = DateTime.UtcNow;
			var validKey = await shopEntities.UserApiKeys
				.Include(k => k.User)
				.SingleOrDefaultAsync(k => k.ValidFrom <= now
				                      && now < k.ValidTo && k.ApiKey == apiKey);
			if (validKey == null)
			{
				return AuthenticateResult.Fail("Invalid API key");
			}

			var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
			{
				new Claim(ClaimTypes.NameIdentifier, validKey.UserId.ToString()),
				new Claim(ClaimTypes.Name, validKey.User.Name)
			}, ApiKeyAuthenticationDefaults.AuthenticationScheme));

			return AuthenticateResult.Success(
				new AuthenticationTicket(principal, ApiKeyAuthenticationDefaults.AuthenticationScheme));
		}
		else
		{
			return AuthenticateResult.NoResult();
		}
	}
}