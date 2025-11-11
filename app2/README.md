# ASP.NET Web API Modernization Example

Welcome to the **ASP.NET Web API Modernization Example** app. 

This application uses Microsoft SQL Server database and provides a few API endpoints and Swagger UI. The API is secured by API keys sent in the `X-ApiKey` HTTP header. 

The app is built using **.NET Framework 4.7.2** and uses **Entity Framework 6**.

Our goal is to convert it to **.NET 8** and **EF Core** while keeping **100% backwards compatibility** in the API and behavior.
    
## Install Prerequisites

Make sure you have the following software installed:

* Visual Studio 2022 with ASP.NET workload and .NET Framework SDK for version 4.7.2
* Microsoft SQL Server Management Studio (or some other tool to work with SQL databases)

## Run SQL Server Management Studio and ensure connection to the SQL Server

Check whether you have SQL Server LocalDB (it should be installed with Visual Studio) or SQL Server Express. 

* For SQL Server LocalDB, the server name should be `(localdb)\MSSQLLocalDB`

* SQL Server Express is usually at `.\SQLEXPRESS`

Run SQL Server Management Studio and make sure you can connect to the database server.

## Clone the repository

If you haven't done this yet, clone the [GitHub repository](https://github.com/tomasherceg/modernization-workshop) to some folder on your disk.

Navigate to `app2` folder.

## Make a copy of `01-initial-state` folder

The folder contains several snapshost of the modernization process.

Make a copy of the `01-initial-state` folder and name it as `my`.

**This copied folder will be the folder you will work with.**

## Open the solution

Open the solution in your copied (`my`) folder in Visual Studio.

Look up the line 12 in the `ModernizationDemo.Api/web.config` file.

Look for the part `...data source=.\SQLEXPRESS;...` and change there the name of the SQL Server instance.

For SQL Server Local DB, the entire line should be this:

```
<add name="ShopEntities" connectionString="metadata=res://*/ShopEntities.csdl|res://*/ShopEntities.ssdl|res://*/ShopEntities.msl;provider=System.Data.SqlClient;provider connection string=&quot;data source=(localdb)\MSSqlLocalDB;initial catalog=ModernizationDemo;integrated security=True;trustservercertificate=True;MultipleActiveResultSets=True;App=EntityFramework&quot;" providerName="System.Data.EntityClient" />
```

## Run the project and test the Swagger

After you changed the connection string, run the project. It should create the database and populate it with data automatically - it takes a couple of seconds.

Navigate to https://localhost:44316/swagger to see if the application started correctly.

## Try the API endpoints

If you try any API endpoint without the API key, it should return HTTP 401.

Enter the API key `1tke53ptj1p5woi61bkt0jsqd501sm48` in the header.

Make sure the `/api/Orders` endpoint returns HTTP 200 and you can see some orders.

Try the `/api/Products/{id}/priceHistory` with the ID=1. You should get HTTP 200 and see some data.

If you try it with ID=999, you should get HTTP 404.

Now you have the project up and running.

> Now it is a good idea to commit your changes.

## Adding tests for the API

Before we start migration the application to the newest version of .NET, it is a good idea to cover it with tests.

Add a new Test Project to the solution called `ModernizationDemo.Tests`. 

> Feel free to use any framework you know. The example implementation in the `02-cover-with-tests` uses **xUnit**.

## Write the first test

Here is a bunch of useful constants you will need for the first test:

```csharp
public static Uri OldBaseAddress = new Uri("https://localhost:44316/");

public const string SomeValidApiKey = "1tke53ptj1p5woi61bkt0jsqd501sm48";
public const string SomeExpiredApiKey = "awlq0vybp53gbr547lizropajzn0cx07";
```

Now, add a first test that will create an instance of `HttpClient`, set up `X-ApiKey` header to a valid API key, and call the `api/Orders` endpoint. 

Ensure that it returns a success status code and the body can be parsed as JSON. You can use this snippet as an example:

```csharp
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("X-ApiKey", SomeValidApiKey);

var response = await httpClient.GetAsync(new Uri(OldBaseAddress, "api/Orders"));
response.EnsureSuccessStatusCode();

var json = await JsonNode.ParseAsync(await response.Content.ReadAsStreamAsync());
var jsonObject = Assert.IsAssignableFrom<JsonObject>(json);
```

## Run the first test

When running the test, the sample application must be running. 

Right-click on the `ModernizationDemo.Api` project and select **View > View In Browser**. This will run the project on the background without switching Visual Studio to the debug mode, so you will be able to edit and run tests freely.

Try what results the endpoint returns, and try to validate at least the structure of the output and a few values. The endpoints should be deterministic - they should always return the same data in the same order.

* Check that the result contains the `TotalCount` property with value of `56`.

* Check that the `Items` array has 10 elements.

* Check that the `Id` of the first item is `48`.

* Check that the `Status` of the first item is `Completed`. 

> Maintaining tests that check larger outputs is quite uncomfortable. Check out the [CheckTestOutput](https://github.com/exyi/CheckTestOutput) package if you want more sophisticated approach. This project allows you to store the results in text files, and ensure they didn't change. There is also a support for deep-comparison of JSON objects.

## Add more tests

You should cover these situations:

* Calling the `api/Orders` endpoint with an invalid API key produces HTTP 401.

* Calling the `api/Orders` endpoint without any API key also produces HTTP 401.

* Calling the `api/Orders` endpoint with `take=50` option should return 50 results.

* Calling the `api/Orders?skip=10` option should return the second page of results (default page size is 10).

* Calling the `api/Orders?take=50` option should return 50 results.

* Calling the `api/Orders?take=1000` should return HTTP 400 because the page size is too large. You should ensure the response contains the `ModelState` object with an array of errors for the `Take` property.

* Calling the `api/Products/1/priceHistory` should return an array of 9 results. Add a check for the attributes of the first and last item.

* Calling the `api/Products/999/priceHistory` should return HTTP 404 as there is no product with the specified ID.

> You can look in the `02-cover-with-tests` folder for an example.

> If all tests pass, it is a good idea to commit your changes.

## Adding a new ASP.NET Core Project

Now is the time we'll add a new ASP.NET Core application, where the final modernized version will end up.

Because we will use the **side-by-side** migration method, both applications must be running at the same time. 

At the beginning, all functionality is only in the old application, and the new application is empty. During the migration, we'll be pulling pieces of functionality from the old app to the new app. Once everything is in the new app, we'll be able to drop the old one completely.

Add a new ASP.NET Core Empty project in the solution and name it `ModernizationDemo.NewApi`. Use .NET 8.0 or newer.

> For better orientation, you can organize the solution using Solution folders. Right-click on the solution and select **Add > Solution folder**. Name it `Old` and move the `ModernizationDemo.Api` and `ModenizationDemo.Core` projects in it. Then, add another solution folder called `New` and move the `ModernizationDemo.NewApi` project inside.

## Configuring the new ASP.NET Core Project

The new ASP.NET Core project should have only the `Program.cs` file and `appsettings.json`. If there is anything else, you can remove it.

Install the `Yarp.ReverseProxy` NuGet package in the project.

The `Program.cs` file should look like this:

```csharp
var builder = WebApplication.CreateBuilder(args);

// register YARP services
builder.Services.AddHttpForwarder();

var app = builder.Build();

// proxy all requests to the old application
app.MapForwarder("/{**catch-all}", app.Configuration["ProxyTo"])
    .Add(static builder => ((RouteEndpointBuilder)builder).Order = int.MaxValue);

app.Run();
```

Open the `appsettings.json` file and add there the following configuration entry:

```json
  "ProxyTo": "https://localhost:44316/"
```

This will ensure that all HTTP requests that will come to the new project will be just proxied to the old application. 

> The `app.MapForwarder` registration must be the last in the pipeline - we want all requests that are not handled by the new app will fall back to the old one.

## Running both applications

To run both applications at the same time, you need to configure multiple startup projects in Visual Studio. 

Right-click on the solution and select **Configure Startup Projects**.

Ensure the **Multiple startup projects** option is selected and the `ModernizationDemo.Api` and `ModernizationDemo.NewApi` have `Action=Start`.

If you press F5 or Ctrl-F5 (run without debugging), two browser windows should open - each for one application.

## Enhancing the tests to cover both applications

Now, you can update the tests to run twice - once for the old app and once for the new app.

If you use xUnit, you can use the following schema:

```csharp
// constants
public static object[][] BaseAddresses =>
[
    new object[] { new Uri("https://localhost:44316/") },
    new object[] { new Uri("https://localhost:7097/") }
];

// ...

[Theory]
[MemberData(nameof(BaseAddresses))]     // the test will run twice for each entry in BaseAddresses
public async Task ListOrdersTest(Uri baseAddress)
{
    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Add("X-ApiKey", SomeValidApiKey);

    var response = await httpClient.GetAsync(new Uri(baseAddress, "api/Orders"));
    response.EnsureSuccessStatusCode();

    // ...
}
```

Update the tests so they would execute for both URLs.

> You can look in the `03-new-project-with-yarp` folder for an example.

> If all tests pass, it is a good idea to commit your changes.

## Preparing the business layer

The **Entity Framework 6** model and some utility classes are currently defined in the `ModernizationDemo.Core` project.

Because we want to migrate to **Entiry Framework Core** which uses different NuGet packages and its API is different, we don't want to change this project to target the new .NET or use .NET Standard. 

Instead, add a new Class Library project called `ModernizationDemo.NewCore`. Make it target .NET 8 or newer.

During the modernization, we will replicate the data model and the business logic there.

> If you use solution folder, move the project to the `New` solution folder.

## Scaffolding the DbContext

Since we already have the SQL database and the old project was using the Database First approach (that one with the EDMX diagrams), it is usually easier to scaffold the Entity Framework Core model from the existing database. 

> If the old project used the Code First approach, you can consider copying the classes to the new project, fixing the namespaces and adapting the entity metadata and configuration to the EF Core API.

First, install the following NuGet packages in the project:

* `Microsoft.EntityFrameworkCore.SqlServer`
* `Microsoft.EntityFrameworkCore.Design`
* `Microsoft.EntityFrameworkCore.Tools`

Second, open the Package Manager Console window in Visual Studio.

Third, select `ModernizationDemo.NewCore` as **default project**.

And lastly, use the following command. **You may need to replace .\SQLEXPRESS to your SQL Server instance name**:

```powershell
Scaffold-DbContext 'Data Source=.\SQLEXPRESS; Initial Catalog=ModernizationDemo; Trust Server Certificate=true' Microsoft.EntityFrameworkCore.SqlServer -Context ShopEntities
```

If everything goes well, 7 new files should appear in the `ModernizationDemo.NewCore` project.

## Prepare the project for shared code

Before migrating the first controller, we need move the code we'll need in both environments to a shared project. 

Create a new Class Library project called `ModernizationDemo.Models`.

Double-click on the project name and update the project file to target .NET Framework 4.7.2 as well as .NET 8:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFrameworks>net472;net8.0</TargetFrameworks>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <LangVersion>latest</LangVersion>
    </PropertyGroup>
</Project>
```

> Notice the `<TargetFrameworks>` element in the project file - this tells MSBuild to compile the library twice. You can look in the `bin` directory to see that there are `net472` and `net8.0` folders with binaries for each platform.

## Move models to the shared project

Now, move all classes from the `ModernizationDemo.Api/Models` folder in this new project.

You'll get some compile errors, because some classes use the data annotation attributes. 

In .NET Framework, these attributes are defined in a framework assembly in the Global Assembly Cache. In the new .NET, these attributes come from a NuGet package.

We'll need to add conditional references in the project file. Double-click on the `ModernizationDemo.Models` project and append the following declarations inside the `<Project>` element:

```xml
    <ItemGroup Condition="'$(TargetFramework)' != 'net472'">
        <PackageReference Include="System.ComponentModel.Annotations" Version="5.0.0" />
    </ItemGroup>
    <ItemGroup Condition="'$(TargetFramework)' == 'net472'">
        <Reference Include="System.ComponentModel.DataAnnotations" />
    </ItemGroup>
```

## Reference the new project from API projects

The old API project as well as the new API project need to reference this new project. 

Right-click on `ModernizationDemo.Api` and `ModernizationDemo.NewApi` and add a project reference to `ModernizationDemo.Models`. 

If you changed the namespaces to match the project name, make sure the `using` statements in the old API project are updated.

Make sure there are no build errors.

> Now it is a good time to commit your changes.

## Migrating the first controller

Now we are ready to start pulling the functionality from the old ASP.NET project.

Create the `Controllers` folder in the `ModernizationDemo.NewApi` project, and copy the `OrdersController` there.

There will be compile erors you will need to fix:

* Remove the `[ApiKeyAuthorizationFilter]` filter for now - we'll implement it later using the standard ASP.NET Core authentication.

* ASP.NET Core controllers should inherit from `ControllerBase` and have the `[ApiController]` attribute.

* Instead of using the default controller route, we usually use the `[Route("api/[controller]")]` attribute.

* The old ASP.NET Web API inferred HTTP methods from the method names. In ASP.NET Core, you need to use `[HttpGet]`, `[HttpPost]`, or other attributes on controller actions.

* The `[FromUri]` attribute was changed to `[FromRoute]` and `[FromQuery]`. 

* We can use dependency injection instead of creating our own instance of `ShopEntities`. Remove the `using` block and declare constructor for the controller where you request the `ShopEntities` instance:

    ```csharp
    //...
    public class OrdersController : ControllerBase
    {
        private readonly ShopEntities dc;
        public OrdersController(ShopEntities dc) 
        {
            this.dc = dc;
        }

        //...
    }
    ```

Make the changes to get rid of all compile errors.

## Register API controllers

ASP.NET Core API controllers are not registered in the HTTP request pipeline, so there is no way to invoke the migrated controller yet.

Open `Program.cs` in the new API project, and update it like so:

```csproj
var builder = WebApplication.CreateBuilder(args);

// API
builder.Services.AddControllers();

// YARP
builder.Services.AddHttpForwarder();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapForwarder("/{**catch-all}", app.Configuration["ProxyTo"])
    .Add(static builder => ((RouteEndpointBuilder)builder).Order = int.MaxValue);

app.Run();
```

As you can see, we've registered controllers in the service collection, and then added the typical ASP.NET Core pipeline. 

> Notice that the `app.MapControllers` call is done before the YARP middleware. This is very important, otherwise the API requests would be sent to the old app and never reach the migrated controller.

## Register EF Core context

Before we will be able to try the API, we'll need to register EF Core context we generated.

Add the following registration to the first part of `Program.cs` (before the application is built):

```
// EF Core
builder.Services.AddDbContext<ShopEntities>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("ShopEntities"));
});
```

## Authentication

The old API used a custom action filter `[ApiKeyAuthorizationFilter]` to perform the authentication. This filter looked in the HTTP request headers, found the `X-ApiKey` header, and looked the key in the `UserApiKeys` table while checking its validity period.

If the key was valid, a `ClaimsIdentity` object representing the corresponding user was created, with the `NameIdentifier` claim containing the user ID, and the `Name` claim containing the user name. 

This identity was set to the current `HttpContext` as well as to the `Thread.CurrentPrincipal`.

In ASP.NET Core, there is a new extensible authentication mechanism for custom authentication, so let's stick to it.

Every application can declare multiple authentication schemes (the ways of how to perform the authentication). Controllers can then request the particular kind of authentication via the `[Authorize]` attribute. If no scheme is specified, the default one is used. Since we will only register a single scheme, it will be also the default one.

Add the following classes in the project:

```csharp
public class ApiKeyAuthenticationDefaults
{
    public const string AuthenticationScheme = "ApiKey";
}

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly ShopEntities shopEntities;

    public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, ShopEntities shopEntities) : base(options, logger, encoder, clock)
    {
        this.shopEntities = shopEntities;
    }

    public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ShopEntities shopEntities) : base(options, logger, encoder)
    {
        this.shopEntities = shopEntities;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // TODO
    }
}
```

Copy the method body from `[ApiKeyAuthorizationFilter]` into the `HandleAuthenticateAsync` method, and adjust it to the new API.

* Instead of throwing exception for an invalid key, return `AuthenticateResult.Fail("Invlaid API key")`.

* If there was no API key header, return `AuthenticateResult.NoResult`.

* Instead of setting the `ClaimsPrincipal` object in the `HttpContext` and current thread, return `AuthenticateResult.Success(new AuthenticationTicket(principal, ApiKeyAuthenticationDefaults.AuthenticationScheme))`.

> Although the querying API of Entity Framework Core is similar to Entity Framework 6, there are differences in behavior. You'll probably get a `NullReferenceException` because Entity Framework Core doesn't do **lazy loading** by default. Can you figure out the solution other then enabling the support for lazy loading?

## Register and use the authentication handler

To use the new authentication, register the authentication scheme in the first part of `Program.cs`:

```csharp
// Authentication
builder.Services.AddAuthentication()
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationDefaults.AuthenticationScheme, null);
```

Then, add the `[Authorize]` attribute on the new `OrderController`.

## Run the tests

Now run the tests and see what happens.

* The tests for product history endpoint should work, because this controller wasn't migrated yet, so they are served from both applications.

* Some tests for the orders endpoint are broken, becasue ASP.NET Core uses different configuration for JSON serialization.

Let's start with the happy path test that returns an actual list of orders. Compare the output of the new and old orders controller and update the JSON configuration in the ASP.NET Core project to behave the same way:

```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // TODO: update configuration here
    });
```

> You can look in the `04-migrated` folder for the solution.

## Fix the ModelState serialization

If you properly check the format if serialized `ModelState` from the old application, you've noticed that it also changed.

Use the following configuration in `Program.cs` to fix that:

```csharp
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = new Dictionary<string, string[]>();
        foreach (var kvp in context.ModelState)
        {
            errors[kvp.Key] = kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray();
        }
        return new BadRequestObjectResult(new
        {
            Message = "The request is invalid.",
            ModelState = errors
        });
    };
});
```

After this change, all tests should pass.

> Now it is a good idea to commit your changes.

## Migrate the ProductsController

Using the same approach to migrate the `ProductsController` from the old project.

Use the tests to verify the migrated controller works.

> Hint: There are another lazy loading issues, and the original implementation suffers from SELECT 1+N problem. Can you fix it so everything is done with up to 3 queries?

## Swagger UI

The last bit we want to migrate is the Open API generation and Swagger UI. 

In the old application, this was made by using `Swashbuckle` NuGet package. 

In the new .NET, we have two options:

* Use [Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore). The configuration model is different, but the feature set is rather similar.

* Use the new [built-in Open API support in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-9.0). 

Choose either of the options and configure Swagger UI to allow entering the API key and test the endpoints.

> The `04-migrated` folder shows the first option.

## Removing the old project

You can now remove the YARP dependency, service and middleware registration from `Program.cs`, and unload the old API project. 

The tests for the new environment should continue working, as all the functionality has been migrated.

## Deployment

🎉 Congratulations! You managed to migrate your first project from .NET Framework to the new .NET!

In the real world, you'd now need to figure out how to replace the old deployed application with the new one. This depends on various environment constraints. 

It may be wise to prepare a quick way back if anything goes wrong. It is also a good idea to run load tests for the endpoints where you expect the most requests, to be sure that the application will not crash because of being significantly slower than the old version.

