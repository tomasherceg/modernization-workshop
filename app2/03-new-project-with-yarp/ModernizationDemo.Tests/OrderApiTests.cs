using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ModernizationDemo.Tests
{
	public class OrderApiTests : ApiTestsBase
	{
		[Theory]
		[MemberData(nameof(BaseAddresses))]
		public async Task ListOrdersTest(Uri baseAddress)
		{
			var httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("X-ApiKey", SomeValidApiKey);

			var response = await httpClient.GetAsync(new Uri(baseAddress, "api/Orders"));
			response.EnsureSuccessStatusCode();

			var json = await JsonNode.ParseAsync(await response.Content.ReadAsStreamAsync());
			var jsonObject = Assert.IsAssignableFrom<JsonObject>(json);

			// validate TotalCount
			var totalCount = Assert.IsAssignableFrom<JsonValue>(jsonObject["TotalCount"]);
			Assert.Equal(JsonValueKind.Number, totalCount.GetValueKind());
			Assert.Equal(56, totalCount.GetValue<int>());

			// validate Items
			var items = Assert.IsAssignableFrom<JsonArray>(jsonObject["Items"]);
			Assert.Equal(10, items.Count);

			// validate first item
			var firstItem = Assert.IsAssignableFrom<JsonObject>(items[0]);

			// validate Id
			var firstItemId = Assert.IsAssignableFrom<JsonValue>(firstItem["Id"]);
			Assert.Equal(JsonValueKind.Number, firstItemId.GetValueKind());
			Assert.Equal(48, firstItemId.GetValue<int>());

			// validate Created
			var firstItemCreated = Assert.IsAssignableFrom<JsonValue>(firstItem["Created"]);
			Assert.Equal(JsonValueKind.String, firstItemCreated.GetValueKind());
			Assert.Equal("2026-01-18T03:43:10.033", firstItemCreated.GetValue<string>());

			// validate Status
			var firstItemStatus = Assert.IsAssignableFrom<JsonValue>(firstItem["Status"]);
			Assert.Equal(JsonValueKind.String, firstItemStatus.GetValueKind());
			Assert.Equal("Completed", firstItemStatus.GetValue<string>());
		}

		[Theory]
		[MemberData(nameof(BaseAddresses))]
		public async Task Unauthenticated(Uri baseAddress)
		{
			var httpClient = new HttpClient();

			var response = await httpClient.GetAsync(new Uri(baseAddress, "api/Orders"));
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Theory]
		[MemberData(nameof(BaseAddresses))]
		public async Task InvalidKey(Uri baseAddress)
		{
			var httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("X-ApiKey", SomeExpiredApiKey);

			var response = await httpClient.GetAsync(new Uri(baseAddress, "api/Orders"));
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Theory]
		[MemberData(nameof(BaseAddresses))]
		public async Task ArgumentValidation(Uri baseAddress)
		{
			var httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("X-ApiKey", SomeValidApiKey);

			var response = await httpClient.GetAsync(new Uri(baseAddress, "api/Orders?take=1000"));
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

			var json = await JsonNode.ParseAsync(await response.Content.ReadAsStreamAsync());
			var jsonObject = Assert.IsAssignableFrom<JsonObject>(json);

			// validate ModelState.Take error message
			var modelState = Assert.IsAssignableFrom<JsonObject>(jsonObject["ModelState"]);
			var take = Assert.IsAssignableFrom<JsonArray>(modelState["Take"]);
			Assert.Single(take);
			var errorMessage = Assert.IsAssignableFrom<JsonValue>(take[0]);
			Assert.Equal(JsonValueKind.String, errorMessage.GetValueKind());
			Assert.Equal("The field Take must be between 10 and 500.", errorMessage.GetValue<string>());
		}
	}
}