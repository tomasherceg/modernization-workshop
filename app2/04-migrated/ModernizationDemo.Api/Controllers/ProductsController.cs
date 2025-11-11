using ModernizationDemo.Api.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using ModernizationDemo.Core;
using ModernizationDemo.Models;

namespace ModernizationDemo.Api.Controllers
{
	[ApiKeyAuthorizationFilter]
	public class ProductsController : ApiController
	{
		[Route("api/Products/{id}/priceHistory")]
		public List<ProductPriceHistoryModel> GetProductPriceHistory(int id)
		{
			using (var dc = new ShopEntities())
			{
				var product = dc.Products.Find(id);
				if (product == null)
				{
					throw new HttpResponseException(HttpStatusCode.NotFound);
				}

				var orderedItems = dc.OrderItems
					.Where(i => i.ProductId == id)
					.ToList();

				return product.ProductSalePrices
					.OrderBy(p => p.ValidFrom)
					.Select(p => new ProductPriceHistoryModel
					{
						Id = p.Id,
						ValidFrom = p.ValidFrom,
						ValidTo = p.ValidTo,
						Price = p.Price,
						TotalQuantity = orderedItems
							.Where(i => p.ValidFrom <= i.Order.Created && i.Order.Created < p.ValidTo)
							.Sum(i => i.Quantity)
					})
					.ToList();
			}
		}

	}
}