namespace ModernizationDemo.Tests;

public abstract class ApiTestsBase
{
	public static object[][] BaseAddresses =>
	[
		new object[] { new Uri("https://localhost:44316/") },
		new object[] { new Uri("https://localhost:7097/") }
	];


	public const string SomeValidApiKey = "1tke53ptj1p5woi61bkt0jsqd501sm48";
	public const string SomeExpiredApiKey = "awlq0vybp53gbr547lizropajzn0cx07";

}