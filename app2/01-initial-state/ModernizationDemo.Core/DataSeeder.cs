using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bogus;

namespace ModernizationDemo.Core
{
	public class DataSeeder
	{
		public static readonly DateTime BaseDate = new DateTime(2025, 11, 1);

		public static void EnsureData()
		{
			Randomizer.Seed = new Random(42);

			using (var db = new ShopEntities())
			{
				db.Database.CreateIfNotExists();

				if (db.Users.Any())
				{
					return;
				}

				var faker = new Faker();
				var users = SeedUsers(db, faker);
				var products = SeedProducts(db, faker);
				SeedOrders(db, faker, users, products);

				db.SaveChanges();
			}
		}

		private static List<User> SeedUsers(ShopEntities db, Faker faker)
		{
			var users = new List<User>();
			for (var i = 0; i < 20; i++)
			{
				var user = new User()
				{
					Name = faker.Company.CompanyName()
				};

				var apiKeyCount = faker.Random.Int(0, 3);
				var date = faker.Date.Soon(100, BaseDate);
				for (var j = 0; j < apiKeyCount; j++)
				{
					var apiKey = new UserApiKey()
					{
						ValidFrom = date.AddYears(-1),
						ValidTo = date,
						ApiKey = faker.Random.AlphaNumeric(32)
					};
					user.UserApiKeys.Add(apiKey);

					date = date.AddYears(-1);
				}

				users.Add(user);
			}

			db.Users.AddRange(users);
			return users;
		}

		private static List<Product> SeedProducts(ShopEntities db, Faker faker)
		{
			var products = new List<Product>();

			for (var i = 0; i < 50; i++)
			{
				var product = new Product()
				{
					Name = faker.Commerce.ProductName(),
					Description = faker.Commerce.ProductDescription(),
					ImageUrl = faker.Image.PicsumUrl()
				};

				var priceCount = faker.Random.Int(1, 10);

				var date = faker.Date.Soon(100, BaseDate);
				var basePrice = faker.Random.Decimal(5, 500);
				for (var j = 0; j < priceCount; j++)
				{
					var newDate = date.AddDays(faker.Random.Int(-200, -20));

					var price = new ProductSalePrice()
					{
						ValidFrom = newDate,
						ValidTo = date,
						Price = Math.Round(basePrice * faker.Random.Decimal(0.75m, 1.25m), 2)
					};
					product.ProductSalePrices.Add(price);
					
					date = newDate;
				}
				products.Add(product);
			}

			db.Products.AddRange(products);
			return products;
		}

		private static List<Order> SeedOrders(ShopEntities db, Faker faker, List<User> users, List<Product> products)
		{
			var minPossibleDate = products.Min(p => p.ProductSalePrices.Min(x => x.ValidFrom));
			var maxPossibleDate = products.Max(p => p.ProductSalePrices.Max(x => x.ValidTo));

			var orders = new List<Order>();

			for (var i = 0; i < 1000; i++)
			{
				var date = faker.Date.Between(minPossibleDate, maxPossibleDate);
				var availableProducts = products
					.Where(p => p.ProductSalePrices.Any(x => x.ValidFrom <= date && date < x.ValidTo))
					.ToList();
				if (!availableProducts.Any())
				{
					continue;
				}

				var canceled = faker.Random.Double() < 0.1;
				var competedDate = (DateTime?)faker.Date.Soon(10, date);
				var order = new Order()
				{
					User = faker.PickRandom(users),
					Created = date,
					Canceled = canceled ? competedDate : null,
					Completed = canceled ? null : competedDate
				};

				var pickedProducts = faker.PickRandom(availableProducts, faker.Random.Int(1, Math.Min(10, availableProducts.Count)));
				foreach (var product in pickedProducts)
				{
					var price = product.ProductSalePrices
						.Where(x => x.ValidFrom <= date && date < x.ValidTo)
						.OrderByDescending(x => x.ValidFrom)
						.Single();
					var quantity = Math.Round(faker.Random.Decimal(1, 50), 4);
					var totalPrice = Math.Round(price.Price * quantity, 2);

					order.OrderItems.Add(new OrderItem()
					{
						Product = product,
						Quantity = quantity,
						ItemPrice = price.Price,
						TotalPrice = totalPrice
					});
					order.TotalPrice += totalPrice;
				}

				orders.Add(order);
			}

			db.Orders.AddRange(orders);
			return orders;
		}
	}
}
