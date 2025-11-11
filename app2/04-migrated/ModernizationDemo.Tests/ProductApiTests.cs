using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ModernizationDemo.Tests
{
	public class ProductApiTests : ApiTestsBase
	{
		[Theory]
		[MemberData(nameof(BaseAddresses))]
		public async Task PriceHistoryTest(Uri baseAddress)
		{
			var httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("X-ApiKey", SomeValidApiKey);

			var response = await httpClient.GetAsync(new Uri(baseAddress, "api/Products/1/priceHistory"));
			response.EnsureSuccessStatusCode();

			var json = await JsonNode.ParseAsync(await response.Content.ReadAsStreamAsync());
			var jsonArray = Assert.IsAssignableFrom<JsonArray>(json);
			Assert.Equal(9, jsonArray.Count);

			// validate first item
			var firstItem = Assert.IsAssignableFrom<JsonObject>(jsonArray[0]);

			// validate Id
			var firstItemId = Assert.IsAssignableFrom<JsonValue>(firstItem["Id"]);
			Assert.Equal(JsonValueKind.Number, firstItemId.GetValueKind());
			Assert.Equal(9, firstItemId.GetValue<int>());

			// validate ValidFrom
			var firstItemValidFrom = Assert.IsAssignableFrom<JsonValue>(firstItem["ValidFrom"]);
			Assert.Equal(JsonValueKind.String, firstItemValidFrom.GetValueKind());
			Assert.Equal("2023-06-11T15:01:01.083", firstItemValidFrom.GetValue<string>());

			// validate Price
			var firstItemPrice = Assert.IsAssignableFrom<JsonValue>(firstItem["Price"]);
			Assert.Equal(JsonValueKind.Number, firstItemPrice.GetValueKind());
			Assert.Equal(215.29, firstItemPrice.GetValue<double>());

			// validate last item
			var lastItem = Assert.IsAssignableFrom<JsonObject>(jsonArray.Last());

			// validate Id
			var lastItemId = Assert.IsAssignableFrom<JsonValue>(lastItem["Id"]);
			Assert.Equal(JsonValueKind.Number, lastItemId.GetValueKind());
			Assert.Equal(1, lastItemId.GetValue<int>());

			// validate ValidFrom
			var lastItemValidFrom = Assert.IsAssignableFrom<JsonValue>(lastItem["ValidFrom"]);
			Assert.Equal(JsonValueKind.String, lastItemValidFrom.GetValueKind());
			Assert.Equal("2025-09-27T15:01:01.083", lastItemValidFrom.GetValue<string>());

			// validate Price
			var lastItemPrice = Assert.IsAssignableFrom<JsonValue>(lastItem["Price"]);
			Assert.Equal(JsonValueKind.Number, lastItemPrice.GetValueKind());
			Assert.Equal(244.23, lastItemPrice.GetValue<double>());
		}

		[Theory]
		[MemberData(nameof(BaseAddresses))]
		public async Task NonExistentProductPriceHistory(Uri baseAddress)
		{
			var httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("X-ApiKey", SomeValidApiKey);

			var response = await httpClient.GetAsync(new Uri(baseAddress, "api/Products/9999/priceHistory"));
			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		}
	}
}