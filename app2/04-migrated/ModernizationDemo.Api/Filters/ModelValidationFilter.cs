using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Http.ModelBinding;

namespace ModernizationDemo.Api.Filters
{
	public class ModelValidationFilter : ActionFilterAttribute
	{
		public override void OnActionExecuting(HttpActionContext actionContext)
		{
			if (!actionContext.ModelState.IsValid)
			{
				actionContext.Response = actionContext.Request.CreateErrorResponse(
					System.Net.HttpStatusCode.BadRequest,
					actionContext.ModelState);
			}
			base.OnActionExecuting(actionContext);
		}
	}
}