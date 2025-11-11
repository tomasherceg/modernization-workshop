using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using ModernizationDemo.Models;
using ModernizationDemo.NewCore;

namespace ModernizationDemo.NewApi.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProductsController(ShopEntities dc) : ControllerBase
{

	[HttpGet("{id}/priceHistory")]
	public async Task<ActionResult<List<ProductPriceHistoryModel>>> GetProductPriceHistory(int id)
	{
		var product = await dc.Products
			.Include(p => p.ProductSalePrices)
			.SingleOrDefaultAsync(p => p.Id == id);
		if (product == null)
		{
			return NotFound();
		}

		var orderedItems = await dc.OrderItems
			.Where(i => i.ProductId == id)
			.Select(i => new
			{
				OrderCreatedDate = i.Order.Created,
				Quantity = i.Quantity
			})
			.ToListAsync();

		return product.ProductSalePrices
			.OrderBy(p => p.ValidFrom)
			.Select(p => new ProductPriceHistoryModel()
			{
				Id = p.Id,
				ValidFrom = p.ValidFrom,
				ValidTo = p.ValidTo,
				Price = p.Price,
				TotalQuantity = orderedItems
					.Where(i => p.ValidFrom <= i.OrderCreatedDate && i.OrderCreatedDate < p.ValidTo)
					.Sum(i => i.Quantity)
			})
			.ToList();
	}

}