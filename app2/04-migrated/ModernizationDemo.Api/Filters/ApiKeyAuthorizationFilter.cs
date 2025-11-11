using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace ModernizationDemo.Api.Filters
{
	public class ApiKeyAuthorizationFilter : ActionFilterAttribute
	{
		public override void OnActionExecuting(HttpActionContext actionContext)
		{
			if (actionContext.Request.Headers.TryGetValues("X-ApiKey", out var apiKeyValues))
			{
				var apiKey = apiKeyValues.First();
				using (var db = new Core.ShopEntities())
				{
					var now = DateTime.UtcNow;
					var validKey = db.UserApiKeys
						.SingleOrDefault(k => k.ValidFrom <= now
							&& now < k.ValidTo && k.ApiKey == apiKey);
					if (validKey == null)
					{
						throw new HttpResponseException(HttpStatusCode.Unauthorized);
					}

					var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
					{
						new Claim(ClaimTypes.NameIdentifier, validKey.UserId.ToString()),
						new Claim(ClaimTypes.Name, validKey.User.Name)
					}, "ApiKey"));
					HttpContext.Current.User = principal;
					Thread.CurrentPrincipal = principal;
				}
			}
			else
			{
				throw new HttpResponseException(HttpStatusCode.Unauthorized);
			}

			base.OnActionExecuting(actionContext);
		}
	}
}