using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
using ModernizationDemo.Api.Filters;
using ModernizationDemo.Api.Models;
using ModernizationDemo.Core;

namespace ModernizationDemo.Api.Controllers
{
	[ApiKeyAuthorizationFilter]
	public class OrdersController : ApiController
	{
		private int CurrentUserId => int.Parse(((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier).Value);

		// GET api/orders
		public PagedList<OrderListModel> Get([FromUri] PagingInfo pagingInfo)
		{
			using (var dc = new ShopEntities())
			{
				var query = dc.Orders
					.Where(o => o.UserId == CurrentUserId);

				var orders = query
					.Select(o => new OrderListModel()
					{
						Id = o.Id,
						Created = o.Created,
						LastChange = o.Completed ?? o.Canceled,
						Status = o.Completed != null ? OrderStatus.Completed 
							: o.Canceled != null ? OrderStatus.Canceled 
							: OrderStatus.Pending,
						TotalPrice = o.TotalPrice
					})
					.OrderByDescending(o => o.Created)
					.Skip(pagingInfo.Skip)
					.Take(pagingInfo.Take)
					.ToList();

				return new PagedList<OrderListModel>(orders, query.Count());
			}
		}

	}
}
