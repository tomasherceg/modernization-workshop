using Microsoft.AspNetCore.Mvc;
using ModernizationDemo.Models;
using ModernizationDemo.NewCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ModernizationDemo.NewApi.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class OrdersController(ShopEntities dc) : ControllerBase
{
	private int CurrentUserId => int.Parse(((ClaimsIdentity)User.Identity!).FindFirst(ClaimTypes.NameIdentifier)!.Value);

	[HttpGet]
	public PagedList<OrderListModel> Get([FromQuery] PagingInfo pagingInfo)
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