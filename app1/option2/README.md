# ASP.NET Web Forms Side-By-Side Modernization Example

Welcome to the **ASP.NET Web Forms Side-By-Side Modernization Example** app. 

This application contains several ASP.NET Web Forms pages and uses various mechanism, such as authentication, session or caching.

To make it easier, most of the business logic was abstracted away to a separate microservice that exposes a REST API.

The app is built using **.NET Framework 4.7.2**. 

Our goal is to use side-by-side migration while rewriting all pages to **Blazor**.

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

Navigate to `app1/option2` folder.

## Make a copy of `01-initial-state` folder

The folder contains several snapshost of the modernization process.

Make a copy of the `01-initial-state` folder and name it as `my`.

**This copied folder will be the folder you will work with.**

## Open the solution

Open the solution in your copied (`my`) folder in Visual Studio.

Look in the `ModernizationDemo.BackendApi/appsettings.json` file, find the `ConnectionStrings/DB` configuration entry and update the `Data Source=.\SQLEXPRESS` with the name of your SQL Server instance.

For SQL Server Local DB, the entire line should be this:

```
    "DB": "Data Source=(localdb)\\MSSqlLocalDB; Initial Catalog=ModernizationShopDemo; Integrated Security=true; Trust Server Certificate=true"
```

> Don't forget to use the double backslash in the SQL Server instance name - JSON requires escaping of this character. 

## Running the app

The application is split into two projects. `ModernizationDemo.App` is the Web Forms frontend that calls `ModernizationDemo.BackendApi`.

The `ModernizationDemo.BackendClient` is a class library that contains the API client generated using NSwag.

First, right-click on the `ModernizationDemo.BackendApi` project and select **View / View In Browser** option. This will run the backend API on the background, so we will be able to play with the frontend app freely.

Once you run the backend, it should create the database automatically and seed it with some data.

Then, ensure that `ModernizationDemo.App` is set up as startup project, and press F5. The frontend should run and you should see a page with a list of images.

## Testing the app

Feel free to examine the application. Here is a short list of interesting things you can find there:

* The API which returns products does not return prices. Instead, for each product, you need to make a separate API call to obtain a price in a specified currency. This is intentional to simulate a real-world scenario that requires caching. Once the application obtains a price for a particular product, it stores it in an in-memory cache to avoid hitting the API rate limit. The cache is invalidated when the product price is changed in the admin area. You can find the caching logic in the Utils class. 
* The master page contains a currency selector component that stores its value in the ASP.NET session. You can find it in the `PageBase` class – a base class for all ASP.NET Web Forms pages in the application.
* All pages accessing the data use the model binding approach. The older ASP.NET applications will probably use `ObjectDataSource` or manual data binding in the code-behind files.
* Since the price is loaded by a second API call, it does not fit the model binding scenario well. Therefore, I make the second request in the DataBound event of the data controls. This will have to be changed in DotVVM as it has no equivalent for this event.
* The `Admin/ProductDetail.aspx` page contains `GridView` and `FormView` nested in another `FormView`. This requires several calls to `FindControl` to dynamically look for controls in code-behind. You will see how much this is easier in DotVVM where all data are contained in the viewmodel. The page also uses validation, including validation groups, as there are multiple independent forms the user may interact with.
* The `Login.aspx` page contains the `Login` control. Instead of using the Membership providers, the control signs the user in in the `Authenticating` event by verifying the credentials using an API endpoint.
    ```
    User name: admin
    Password:  Admin1234+
    ```
* The access to the administration is restricted by a rule in the `web.config` file.
* The application contains a handler that generates an RSS feed with all products. You can find it in the `Handlers` folder.

## Creating a new ASP.NET Core app

Now is the time we'll add a new ASP.NET Core application, where the final modernized version will end up.

Because we will use the **side-by-side** migration method, both applications must be running at the same time. 

At the beginning, all functionality is only in the old application, and the new application is empty. During the migration, we'll be pulling pieces of functionality from the old app to the new app. Once everything is in the new app, we'll be able to drop the old one completely.

Add a new ASP.NET Core Empty project in the solution and name it `ModernizationDemo.AppNew`. Use .NET 8.0 or newer.

## Configuring the new ASP.NET Core Project

The new ASP.NET Core project should have only the `Program.cs` file and `appsettings.json`. If there is anything else, you can remove it.

Install the `Yarp.ReverseProxy` NuGet package in the project.

The `Program.cs` file should look like this:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthorization();

// register YARP services
builder.Services.AddHttpForwarder();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

// proxy all requests to the old application
app.MapForwarder("/{**catch-all}", app.Configuration["ProxyTo"])
    .Add(static builder => ((RouteEndpointBuilder)builder).Order = int.MaxValue);

app.Run();
```

Open the `appsettings.json` file and add there the following configuration entry:

```json
  "ProxyTo": "http://localhost:52477/"
```

This will ensure that all HTTP requests that will come to the new project will be just proxied to the old application. 

> The `app.MapForwarder` registration must be the last in the pipeline - we want all requests that are not handled by the new app will fall back to the old one.

## Running both applications

To run both applications at the same time, you need to configure multiple startup projects in Visual Studio. 

Right-click on the solution and select **Configure Startup Projects**.

Ensure the **Multiple startup projects** option is selected and the `ModernizationDemo.App`, `ModernizationDemo.AppNew` and `ModernizationDemo.BackendApi` have `Action=Start`.

If you press Ctrl-F5 (run without debugging), three browser windows should open - two of them should show the application's home page , the third will be the results of the backend API endpoint.

## SystemWebAdapters

Before we start migrating the application, we need to set up a common infrastructure:

* **Authentication**: If the user signs in, both applications need to be able to decode the authentication cookie and understand who is the current user.
* **Caching**: The prices of products are saved in an in-memory cache. If we change some product's price in the admin area, the cache in the other application needs to be invalidated.
* **Session**: The user can select in which currency the prices should be shown. This information is stored in ASP.NET Session state, and we'll need to somehow make it available in the new ASP.NET Core app.

Most of these problems are solved by the `Microsoft.AspNetCore.SystemWebAdapters.CoreServices` NuGet package, so install it in the project and add the following registrations in `Program.cs`:

```csharp
// ...

builder.Services.AddSystemWebAdapters();

// ...

app.UseRouting();
app.UseAuthorization();

app.UseSystemWebAdapters();

app.MapForwarder(    // ...
```

## Forwarded headers

The first problem you may discover when testing the new setup locally is that the old application suddenly receives requests for a different domain. You can see this behavior in our example application when you scroll down and click on the RSS Feed link. You will see the <link> and <id> elements reporting the wrong URL in the XML document that appears. It will show `http://localhost:52477` instead of `https://localhost:7220`, which you can see in the browser address bar. 

We need to tell the old project to respect the `X-Forwarded-Proto`, `X-Forwarded-Host` and `X-Forwarded-Prefix` headers that YARP adds to all proxied requests. Luckily, there is a NuGet package for that. 

Install the `Microsoft.AspNetCore.SystemWebAdapters.FrameworkServices` NuGet package **into the old ASP.NET application**.

> To ensure compatibility with the sample app, use the version `1.3.0`.

Open the `Global.asax` file and add the following code snippet to the `Application_Start` handler:

```csharp
protected void Application_Start(object sender, EventArgs e)
{
    // ...

    this.AddSystemWebAdapters()
        .AddProxySupport(options => options.UseForwardedHeaders = true);
}
```

Run the app, open the page https://localhost:7075/products/rss and make sure that the URL reported in the `<link>` element starts with `https://localhost:7075/`.

## Sharing code between the apps

There are some useful classes defined in the Web Forms application that we will need in the new app as well. In real-world scenarios, this can be a large portion of code. There are a couple of options how to do the refactoring:

* **Moving the code to a separate class library**: This is the cleanest choice, however the code cannot reference APIs specific to .NET Framework. For example, if there are many dependencies on ASP.NET Web Forms stuff, it may not be possible. You can target either .NET Standard, or use the `<TargetFrameworks>` element in the project file to compile the same code for multiple runtimes. It is possible to use conditional compilation symbols (such as `#if !NET`) to make the code slightly different for each runtime.

* **Linking files to the new project**: If you just need to share a few files, you can link them to the new project from their original path, without the need to duplicate them. The code will be effectively compiled twice, and same as in the previous case, you can use conditional compilation symbols.

* **Copying the files**: Although this seems as the worst way, it is often inevitable. The number of API changes are so large that the code would be hard to maintain because of the number of required conditional sections.

In our case, we'll just to share a single class - `Utils`. The easiest way is to link it in the new project by adding the following element in the `.csrpoj` file (in the new ASP.NET Core project):

```xml
  <ItemGroup>
    <Compile Include="..\ModernizationDemo.App\Utils.cs" Link="Utils.cs" />
  </ItemGroup>
```

There will be a few compile errors:

* Most of them are removed by adding a reference to the `ModernizationDemo.BackendClient` project. This project targets .NET Standard, so it can be consumed from goth the old and the new app.

* The remaining compile error is caused by calling `Global.GetApiClient()` in the new app, where the `Global` class doesn't exist. Instead of adding it, we'll use a little hack to resolve the API client from the dependency injection, and keep the old implementation for the old .NET app.

Add the `GetApiClient` method in the class and fix the using like this:

```csharp
// ...

#if NET
using Microsoft.AspNetCore.SystemWebAdapters;
#endif 

// ...
            price = GetApiClient().GetProductPrice(productId, selectedCurrency);
// ...

private static ApiClient GetApiClient()
{
#if !NET
    return Global.GetApiClient();
#else
    return System.Web.HttpContext.Current!.AsAspNetCore()
        .RequestServices.GetRequiredService<ApiClient>();
#endif
}
```

Also, add the following registration in `Program.cs`:

```csproj
builder.Services.AddSingleton(_ => new ApiClient(builder.Configuration["Api:Url"], new HttpClient()));
```

Since we reference a configuration value, add it the `appsettings.json` file:

```json
  "Api": {
    "Url": "https://localhost:7211/"
  }
```

> Now it is a good time to commit your changes.

## Migrating the first page

From now on, the process is quite straightforward. We will reimplement all ASP.NET Web Forms pages using a different UI framework, **Blazor Server** in this case, reusing parts of HTML and page-specific backend code. 

> Using **Blazor WebAssembly** would be possible and maybe even easier because we already have our business logic encapsulated as a REST API. However, this is not the usual case in most modernization projects.

Add the necessary declarations in `Program.cs` to enable Blazor:

```csharp
// ...

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// ...

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// ...

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
    
app.MapForwarder(    // ...
```

> The `app.MapForwarder` registration must be the last in the pipeline - we want all requests that are not handled by the new app will fall back to the old one.

## Adding the App component

The code references the `App` component, which needs to be added to the project. 

Create a folder named `Components` and add there a file named `App.razor` with the following structure:

```dothtml
@using Microsoft.AspNetCore.Components.Web
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width,initial-scale=1.0" />
    <base href="/" />
    <script src="Scripts/bootstrap.bundle.min.js"></script>
    <link href="Styles/bootstrap.min.css" rel="stylesheet" />
    <link href="Styles/bootstrap-icons.min.css" rel="stylesheet" />
    <link href="Styles/app.css" rel="stylesheet" />
    <link rel="stylesheet" href="ModernizationDemo.AppNew.styles.css"/>
    <HeadOutlet />
</head>
<body>
<Routes @rendermode="new InteractiveServerRenderMode(prerender: false)"/>
<script src="_framework/blazor.web.js"></script>
</body>
</html>
```

We will also need the `_Imports.razor` and `Routes.razor` files. Copy them from the `03-master-page-and-first-page` folder.

## Migrating the layout page

Before migrating the first page, we must build the layout page. Basically, we will rewrite the `Pages/Site.master` file and its code-behind class. 

Create the `Components/Layout/MainLayout.razor` file, copy the markup from the `Pages/Site.master` in the old project, and clean up the code from `runat="server"` and other ASP.NET Web Forms-specific things.

* Add the following directives at the top of the file:
    ```dothtml
    @using Microsoft.AspNetCore.Components.Authorization
    @using ModernizationDemo.AppNew.Services
    @inject SelectedCurrencyService selectedCurrencyService
    @inherits LayoutComponentBase
    ```
* Since the component will be embedded in `App.razor`, we can safely remove the entire `<head>` section, and keep only the `<div class="container">` element and its contents. 
* Replace the `<asp:LoginView>` component and the subsequent `<li>` element with the following syntax:
    ```dothtml
    <AuthorizeView>
        <li class="nav-item">
            <a class="nav-link" href="/admin/products">Admin</a>
        </li>
    </AuthorizeView>
    <li class="nav-item">
        <AuthorizeView>
            <NotAuthorized>
                <a href="/login" class="nav-link">Sign in</a>
            </NotAuthorized>
            <Authorized>
                <!-- TODO: requires single sign-on -->
                <a href="/" class="nav-link">Sign out</a>
            </Authorized>
        </AuthorizeView>
    </li>
    ```
* Replace the `<asp:DropDownList>` with the following Blazor equivalent:
    ```dothtml
    <select class="form-select me-2"
        @bind="SelectedCurrency"
        @bind:after="() => selectedCurrencyService.SetCurrency(SelectedCurrency)">
        <option value="USD">USD</option>
        <option value="EUR">EUR</option>
        <option value="JPY">JPY</option>
        <option value="GBP">GBP</option>
    </select>
    ```
* Replace the `<asp:ContentPlaceHolder>` control with the `@Body` expression.
* We will need to add the code section to represent the component state:
    ```csharp
    @code {
        public string SelectedCurrency { get; set; }

        protected override void OnInitialized()
        {
            SelectedCurrency = selectedCurrencyService.SelectedCurrency;
            base.OnInitializedAsync();
        }
    }
    ```

## Service to remember the selected currency

You may still see some errors, because the `MainLayout` component references a service to maintain the selected currency we haven't implemented yet.

Create the `Services` folder in the new ASP.NET Core project, and add the following implementation:

```csharp
public class SelectedCurrencyService
{
    public string SelectedCurrency { get; private set; } = "USD";

    public event Action? SelectedCurrencyChanged;

    public void SetCurrency(string currency)
    {
        SelectedCurrency = currency;
        SelectedCurrencyChanged?.Invoke();
    }
}
```

Don't forget to register it in `Program.cs`:

```csharp
// ...

builder.Services.AddScoped<SelectedCurrencyService>();

// ...
```

Now the layout component shouldn't have any errors.

## Migrating the product detail page

Let's migrate the first page! Create the `Components/Pages/ProductDetail.razor` file and use the markup from `Pages/ProductDetail.aspx`.

Since there is no `FormView` equivalent, copy only the contents of the `<div class="row">` element.

* Use the following directives at the beginning of the page.
    ```dothtml
    @page "/product/{id:guid}"
    @using ModernizationDemo.App
    @using ModernizationDemo.AppNew.Services
    @using ModernizationDemo.BackendClient
    @inject ApiClient apiClient
    @inject SelectedCurrencyService selectedCurrencyService
    ```
* We can adapt the Web Forms page's code-behind to the following component state representation:
    ```csharp
    @code {
        [Parameter]
        public Guid Id { get; set; }

        public ProductModel? Product { get; set; }

        public string? ProductPrice { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Product = await apiClient.GetProductAsync(Id);
            ProductPrice = Utils.GetProductPriceWithCaching(Product.Id, selectedCurrencyService.SelectedCurrency);
            await base.OnInitializedAsync();
        }
    }
    ```    
* Wrap the contents of the `<div class="row">` element in the `@if` block. We'll be loading the data from the API asynchronously and until the data is available, the `Product` property will be `null`:
    ```@dothtml
    <div class="row">
        @if (Product is not null)
        {
            ...
        }
    </div>
    ```
    In today's applications, displaying "skeleton loading" animations is common to indicate that content is being loaded.
* Replace the ASP.NET Web Forms data-bindings with Razor ones:
    ```dothtml
    <!-- Web Forms data-binding -->
    <%# Eval("ImageUrl") %>

    <!-- Razor data-biding -->
    @Product.ImageUrl
    ```
* Replace the `PriceLiteral` with `@ProductPrice` expression.

Notice that we are calling the `Utils` class that we linked from the old application earlier to obtain the product price from the cache. It uses the `HttpRuntime.Cache` static member, and if you try to run the example application, you can see it works – the Web Forms Adapters library provides an implementation for this cache.

> If you run into any issue, refer to the `03-master-page-and-first-page` folder.

## Testing the app

Now you should be able to run all apps and try out the new product detail page.

Make sure you try it from the https://localhost:7075/ domain, which is the new ASP.NET Core app. Remember that the old version of the page will still work on the old domain.

However, we still have a problem - if you change the currency on the home page and then navigate to the product detail page, you will see that the value is lost.

This is because our `SelectedCurrencyService` is storing the selected currency in its `SelectedCurrency` property, and since the service is registered as **scoped**, it will get lost once you navigate away from the Blazor app (which is currently only one page, so the user will likely enter and leave the Blazor app many times).

## Sharing the session

In the old app, the selected currency is stored in ASP.NET Session State. By default, the objects stored in the session state are kept in the server memory (there are other options, such as storing them in a database), and the session ID is stored in a cookie. When the user makes the next request, the session can be easily identified by looking at this cookie.

The session in Blazor is quite difficult to use. In Blazor WebAssembly, the application runs entirely in the browser, while the session is a server-side concept - thus, it cannot be used on the client at all. In Blazor Server, you can theoretically use it as the code runs on the server, but there are many limitations. 

The first issue is that you need to set the cookie holding the session ID immediately at the first GET request when entering the application. If you try to do it later, the server has no way to send the cookie to the client, as the `HttpContext` at that moment represents the SignalR connection for the particular client. Cookies cannot be transferred over this channel – they must be sent to the client using the HTTP `Set-Cookie` response header. For security reasons, the session cookie is set as HTTP-only to prevent accessing it from JavaScript. 

Even if you use other technology than Blazor (ASP.NET Core MVC or Razor Pages, for example), you will have to deal with another problem. Because both applications have different implementations of the session, the data is not synchronized, and many features may not work correctly when the user transitions between the applications. 

The `SystemWebAdapters` packages can do more than just implement the old `System.Web` API to work in ASP.NET Core. The .NET Framework package `Microsoft.AspNetCore.SystemWebAdapters.FrameworkServices` we used to deal with forwarder headers earlier, can provide the "remote app session" experience. Basically, it establishes a secure communication channel the application can use to exchange or synchronize session information. 

There is another problem - a lot of legacy applications store various objects in a session, and because these objects are kept in memory, they can be really anything. We now need to transfer objects between two applications, so we need them to be JSON-serializable. This may require significant changes in the old application.

## Set up remote session

To start using Remote app session, add the following registration to `Program.cs` in the new app:

```csharp
// ...

builder.Services.AddSystemWebAdapters()
    .AddJsonSessionSerializer(options =>
    {
        options.RegisterKey<string>("SelectedCurrency");
    })
    .AddRemoteAppClient(options =>
    {
        options.RemoteAppUrl = new(builder.Configuration["ProxyTo"]);
        options.ApiKey = builder.Configuration["RemoteAppApiKey"];
    })
    .AddSessionClient();

// ...
```

You will need to add an API key to `appsettings.json`:

```json
  "RemoteAppApiKey": "SOME_SECRET_VALUE"
```

Also, add the following configuration to `Global.asax` in the old application:

```csharp
protected void Application_Start(object sender, EventArgs e)
{
    // ...

    this.AddSystemWebAdapters()
        .AddProxySupport(options => options.UseForwardedHeaders = true)
        .AddJsonSessionSerializer(options =>
        {
            options.RegisterKey<string>("SelectedCurrency");
        })
        .AddRemoteAppServer(options => 
        {
            options.ApiKey = ConfigurationManager.AppSettings["RemoteAppApiKey"];
        })
        .AddSessionServer();
}
```

Finally, add the same API key to the `<appSettings>` section in `web.config`:

```xml
    <add key="RemoteAppApiKey" value="SOME_SECRET_VALUE" />
```

> If you commit the secret key in git (it is not easy to treat secrets properly in .NET Framework projects), make sure you use a different value in production deployments!

This will expose the API endpoint, providing the new application with access to the session data. The session information will still be stored in the old ASP.NET application as it was before, but now, the new application will be able to access it.

## Using the remote session

As it was mentioned before, the session is tricky:

1. Because of performance reasons, the session is not loaded by default – you have to request it explicitly. If you use ASP.NET Core MVC or API controllers, you can use the `[Session]` attribute to indicate your code will need the session. Make sure you specify the `IsReadOnly` parameter if you do not plan to modify the session in order to prevent unnecessary locking. If you use the minimal API approach or want to request the session for all controller actions, you can call `RequireSystemWebAdapterSession` after calling the particular `Map*` method in `Program.cs`. Both approaches will instruct the `SystemWebAdapters` library to load the session at the beginning of the request pointing to the particular URL and commit the changes when such request ends. 
2. The session will be accessible only through the old ASP.NET API – `System.Web.HttpContext.Current.Session["key"]`. Once you finish the migration, you may either transition to using ASP.NET Core Session, or reconfigure `SystemWebAdapters` to use the wrapped session mode – it will start storing the information in ASP.NET Core session. 

First, copy the `ModernizationDemo.AppNew/Services/BlazorSessionExtensions.cs` class from the `03-master-page-and-first-page` example and place it in your `Services` folder. This class contains several helper methods to ensure session works in Blazor Server.

Then, add the following code in `Program.cs`:

```csharp
// ... 
app.UseSystemWebAdapters();

// TODO: After completing the migration, replace the session with a better mechanism for Blazor
app.LoadSessionForBlazorServer();

// ...
```

Basically, this will make sure the session is loaded at the beginning of every Blazor Server connection (when the server receives the `CONNECT` HTTP command). The session must be loaded in the read-only mode, otherwise it would be locked for other usages, for example, if the user has have the app open in multiple browser tabs.

When we'll need to modify the session, we should reload it again with the writable flag, and store the new value immediately to reduce the time for which the session is locked.

Update the `SelectedCurrencyService` like this:

```csharp
public class SelectedCurrencyService(IHttpContextAccessor httpContextAccessor)
{
    private string? selectedCurrency;
    public event Action? SelectedCurrencyChanged;

    public async Task<string> GetCurrency()
    {
        if (selectedCurrency == null)
        {
            selectedCurrency = System.Web.HttpContext.Current.Session["SelectedCurrency"] as string ?? "USD";
        }
        return selectedCurrency;
    }

    public async Task SetCurrency(string currency)
    {
        selectedCurrency = currency;
        SelectedCurrencyChanged?.Invoke();

        using (var sessionLock = await httpContextAccessor.HttpContext.AcquireSessionLock())
        {
            // modify the session state here
            System.Web.HttpContext.Current.Session["SelectedCurrency"] = currency;

            await sessionLock.CommitAsync();
        }
    }
}
```

As you can see, the `GetCurrency` method looks in the session state using the old API to obtain the value. The `SetCurrency` method was made asynchronous and calls the `AcquireSessionLock` helper method to reload the session for writing. After the value is changed, the `CommitAsync` method is called to immediately send the changes to the old application.

We'll need to update the `OnInitialized` method in `MainLayout.razor`:

```csharp
    // ...

    protected override async Task OnInitializedAsync()
    {
        SelectedCurrency = await selectedCurrencyService.GetCurrency();
        await base.OnInitializedAsync();
    }
```

Also, the `<select>` element in `MainLayout.razor` should be updated to use the asynchronous version:

```dothtml
<select class="form-select me-2"
        @bind="SelectedCurrency"
        @bind:after="async () => await selectedCurrencyService.SetCurrency(SelectedCurrency)">
```

You'll also need to make one trivial fix in `ProductDetail.razor`.

## Testing the app

You should now be able to run the app and see the changes in selected currency to survive between the home page and the product detail page.

> Now it is a good time to commit the changes.

## Sharing the authentication state

If you sign in to the old application using the Forms authentication, ASP.NET creates an authentication cookie (named `.ASPXAUTH` by default) to store the encrypted ticket containing the user name, expiration date, and other metadata. The ability to decrypt and verify the ticket depends on the machine keys, a concept that is not present in the new .NET.

The `SystemWebAdapters` library provides a mechanism for sharing the authentication state as we saw in the previous sections about sessions. This method works if your application uses Forms authentication and other authentication types that use `ClaimsPrincipal` and `ClaimsIdentity` objects. 

Add the following registration to the new app's `Program.cs` file:

```csproj
// ...
builder.Services.AddSystemWebAdapters()
    //...
    .AddAuthenticationClient(isDefaultScheme: true);
// ...
```

Next, add the following registration to the old app's `Global.asax` file:

```csharp
// ...
this.AddSystemWebAdapters()
    // ...
    .AddAuthenticationServer();
// ...
```

There is one caveat concerning the sign-out functionality. If you are in the new application, the sign-in button is usually a link that points to the login page (e.g. `/login`). However, the sign-out is a button that calls the `SignOut` method in the master page using ASP.NET Web Forms postback mechanism. The easiest solution is to create a `Logout.aspx` page in the old project and map it to the `/logout` URL. 

Add the `Logout.aspx` page in the old application. Keep the markup empty, and place the following event handler in its code-behind:

```csharp
protected void Page_Load(object sender, EventArgs e)
{
    FormsAuthentication.SignOut();
    Response.Redirect("~/");
}
```

Don't forget to register the route for the new Web Forms page in `Global.asax`:

```csharp
// ...
RouteTable.Routes.MapPageRoute("Logout", "logout", "~/Pages/Logout.aspx");
// ...
```

Then, update the sign-out button in `MainLayout.razor` to link to the logout page:

```dothtml
<AuthorizeView>
    <NotAuthorized>
        <a href="/login" class="nav-link">Sign in</a>
    </NotAuthorized>
    <Authorized>
        <a href="/logout" class="nav-link">Sign out</a>
    </Authorized>
</AuthorizeView>
```

> Now it is a good time to commit your changes.

## Cache invalidation

The last bit of shared infrastructure is cache invalidation. When you update the product price in the old application, the new application's product detail page may not be aware of this change.

One option is to use a distributed cache, but that would usually require significant changes in the legacy app, which we want to avoid. Also, the old ASP.NET caching API is synchronous, which doesn't play well with distributed caches that may even sit on a different server.

Instead, we'll use a custom solution for signalling changes in cached data. This solution will be based on SignalR, as we may need to run both old and new application in multiple instances.

Copy the `ModernizationDemo.AppNew/Migration/CacheInvalidationHub.cs` file from the `03-master-page-and-first-page` folder to the same path in your new app.

Register the hub in the new app's `Program.cs`:

```csharp
// ...

app.UseSystemWebAdapters();
app.LoadSessionForBlazorServer();

app.MapHub<CacheInvalidationHub>("_migration/cache-invalidate")
    .ShortCircuit();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ...
```

> The `ShortCircuit` call that skips the authentication and authorization middlewares. This is necessary because of a bug in the Remote authentication feature of `SystemWebAdapters` that throws exceptions when the Hub tries to authenticate using the default scheme. The hub is checking the API key itself.

Next, copy the `ModernizationDemo.App/Migration/CacheInvalidationClient.cs` file from the `03-master-page-and-first-page` folder to the same path in your old app.

Install the `Microsoft.AspNetCore.SignalR.Client` NuGet package in the old ASP.NET app.

You will also need to add the configuration entry to the `<appSettings>` element in `web.config`:

```xml
    <add key="RemoteAppUrl" value="https://localhost:7075" />
```

Then, register the client in `Global.asax`:

```csharp
protected void Application_Start(object sender, EventArgs e)
{
    // ...
    CacheInvalidationClient.StartWorker();
}

protected void Application_End(object sender, EventArgs e)
{
    CacheInvalidationClient.StopWorker();
}
```

Finally, we need to update the `Utils` class to call invalidate the cache when we update the underlying data:

```csharp
#if !NET
using ModernizationDemo.App.Migration;
#else
using ModernizationDemo.AppNew.Migration;
using Microsoft.AspNetCore.SignalR;
#endif

// ...

public static void NotifyCacheEntryInvalidated(string cacheKey)
{
#if !NET
    CacheInvalidationClient.NotifyCacheEntryInvalidated(cacheKey);
#else
    var hubContext = System.Web.HttpContext.Current!.AsAspNetCore()
       .RequestServices
       .GetRequiredService<IHubContext<CacheInvalidationHub>>();
    CacheInvalidationHub.NotifyCacheEntryInvalidated(hubContext, cacheKey);
#endif 
}
```

> This solution is just a temporary workaround that will disappear once the modernization is completed.

> If you run in trouble, refer to the `03-master-page-and-first-page` folder where the master page and the product detail pages are migrated.

> Now it is a good time to commit your changes.

## Migrate other pages

Now we have everything ready to migrate the remaining pages:

* `Pages/Default.aspx`
* `Pages/Login.aspx`
* `Pages/Admin/Products.aspx`
* `Pages/Admin/ProductDetail.aspx`

Let's take a shortcut now - copy the entire `Components` folder from the `04-all-pages-and-handler` folder to your project.

You will need to install the `Fritz.BlazorWebFormsComponents` NuGet package in the new project.

> Make sure that the `Login` page is migrated as the last one. Since we rely on the old ASP.NET app's authentication state, the new login page will need to be wired up on the ASP.NET Core Cookie authentication.

## Optional reading: Differences between Blazor and Web Forms

If you want to migrate the pages yourself, here are a couple of notes:

* Migration of the home page (`Components/Pages/Default.razor`) is quite easy. It is useful to implement a custom paging control (`Components/Controls/Pager.razor`), which obtains the page index, page size, and the total number of records. It can fire the `OnPageIndexChanged` event when the user switches to another page. I also had to subscribe to the event emitted by `SelectedCurrencyService` to reload the data when the user switches the currency (and unsubscribe in the `Dispose` method to avoid memory leaks).
* To implement the admin product list page (`Components/Pages/Admin/Products.razor`) with ASP.NET Web Forms GridView component, I used the GridView implementation from the `BlazorWebFormsComponents` package I mentioned earlier. Although it does not support all the features of the Web Forms version, it saved quite a lot of work. Instead of using the `SelectMethod` (which does not seem to support asynchronous operations and requires paging parameters even though the control does not support them yet), it is possible to bind the collection of records directly with the `Items` property.
* The implementation of `GridView` in the package supports the most frequently used column types, but unfortunately, you have to specify `ItemType` in each of them – they cannot infer it from the parent control. The `ItemTemplate` must specify `Context="item"` in order to be able to access the object bound to the current row.
* `GridView` does not support paging, but it is not difficult to implement your own paging experience that looks closely enough to what the users had in the Web Forms application. 
* I had to make bigger changes on the admin product detail page in the `Components/Pages/Admin/ProductDetail.razor` file. The page is quite complex as it handles both inserting and editing of products, and also allows inserting, updating, and deleting of product prices for each currency (I wanted to demonstrate a more complicated form that works with dependent entities).
* The Web Forms implementation uses the `FormView` component, which is present in the `BlazorWebFormsComponents` package, but I decided not to use it as it does not add much value. To decide whether the insert or edit version of the main form should be displayed, I simply used an if block.
* To use validation, I wrapped the editable fields in the `EditForm` built-in Blazor component and used `DataAnnotationsValidator` to add support for inferring validation rules from the data annotation attributes. Because I cannot edit the model classes in the `ModernizationDemo.BackendClient` project as they are generated by the OpenAPI tooling, I defined the attributes in separate classes (using the same property names) and used the TypeDescriptor API to tell Blazor to search the validation attributes in these metadata classes.
* I again used the `GridView` control from `BlazorWebFormsComponents` to implement the price editing experience. Because the component does not support the row edit functionality, I had to define my own `EditedCurrency` property and use `TemplateField` with if blocks to render either the read-only or editable field.
* What is a bit unpleasant is that you cannot easily use the Blazor `EditForm` component in tables, as each table cell is in a different <td> element, and there is no way of inserting the <form> element inside the <tr> element. Luckily, I only had one editable field in my grid, so I could put the `EditForm` control in the table cell and use the `EditContext` object directly to call validation in the `UpdatePrice` method.
* The form to insert a new price row was made as a separate `EditFor` under the `GridView`. Because I needed to add a custom validation rule (to prevent users from inserting prices for a currency that is already defined), I had to define `ValidationMessageStore` to be able to add arbitrary validation errors there. See the `InsertPrice` method that contains the validation.

## Replacing HTTP handler that generates the RSS feed

The last dependency on Web Forms is the `ProductsRssHandler` that is responsible for generating the site's RSS feed. 

First, take the old code and try to adapt it to the new ASP.NET Core API:

```csharp
public class ProductsRssHandler(ApiClient apiClient)
{
    public async Task BuildRssFeed(HttpContext context)
    {
        // TODO: port the generating of the SyndicationFeed here

        // writing to the response body must be done asynchronously, but XmlTextWriter doesn't have API for this
        using var ms = new MemoryStream();
        await using var xw = new System.Xml.XmlTextWriter(ms, Encoding.UTF8);
        xw.Formatting = System.Xml.Formatting.Indented;
        var ff = new Atom10FeedFormatter(feed);
        ff.WriteTo(xw);
        xw.Flush();

        ms.Position = 0;
        await ms.CopyToAsync(context.Response.Body);
    }

    // ...
}
```

Don't forget to add the `System.ServiceModel.Syndication` NuGet package.

Now, we can just register the handler in the HTTP pipeline. The easiest way is to use the new Minimal API approach and call `MapGet` to handle GET requests pointing to the `/products/rss` URL:

```csharp
builder.Services.AddScoped<ProductsRssHandler>();

// ...

app.MapGet("/products/rss", 
    (ProductsRssHandler handler, HttpContext context) => handler.BuildRssFeed(context));

// ...
```

The arguments of the lambda expression will be supplied automatically using the dependency injection.

> If you run in trouble, refer to the `04-all-pages-and-handler` folder where the master page and the product detail pages are migrated.

> Now it is a good idea to commit your changes.

## Moving the static files

There are still a few artifacts in the old ASP.NET app in use - for example, the static files.

Ensure you have the `wwwroot` folder in your new project, and move there the following folders from the old project.

* `Styles`
* `Scripts`

## Removing remote session

Once you reach the point when all functionality is migrated to the new ASP.NET Core project, you can start getting rid of the old project. Please note that this process is not as simple as removing the project in Visual Studio. Some features of `SystemWebAdapters` library, such as remote authentication or session, still depend on the old application being up and running.

First, remove the session from `SelectedCurrencyService`:

```csproj
public class SelectedCurrencyService
{
    public string SelectedCurrency { get; private set; } = "USD";

    public event Action? SelectedCurrencyChanged;

    public Task<string> GetCurrency()
    {
        return Task.FromResult(SelectedCurrency);
    }

    public Task SetCurrency(string currency)
    {
        SelectedCurrency = currency;
        SelectedCurrencyChanged?.Invoke();
        return Task.CompletedTask;
    }
}
```

You can also:

* Delete the `BlazorSessionExtensions` class and the `LoadSessionForBlazorServer` usage in `Program.cs`.
* Remove the session configuration of `SystemWebAdapters` from `Program.cs` - keep only `AddRemoteClient` and `AddAuthenticationClient`:
    ```csharp
    // ...

    builder.Services.AddSystemWebAdapters()
        .AddRemoteClient(...)
        .AddAuthenticationClient(...);

    // ...

    app.UseSystemWebAdapters();

    // ...
    ```

## Cookie Authentication

Since out login page currently doesn't work, we need to migrate to the new ASP.NET Core cookie authentication.

Register it in `Program.cs` and remove the `AddAuthenticationClient` call of `SystemWebAdapters`.

```csharp
// ...

    builder.Services.AddSystemWebAdapters()
        .AddRemoteClient(...);

// ... 

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, 
        options =>
        {
            options.LoginPath = "/login";
        });

// ...
```

We have to implement the sign-in functionality using ASP.NET Core cookies in `Components/Pages/Login.razor`. Because the call to `HttpContext.SignInAsync` needs to set the authentication cookie, we need to fine-tune two things:
* Currently, we set the server-side interactive mode without server prerendering for the entire application. We have to do it for all pages except for the `/login` page, which will need to run in the server-rendered mode. When we submit the form, it must not be done interactively using the SignalR channel. Instead, it must be the classic HTTP POST request.
* If the user navigates to the page from another Blazor page, it will be loaded using the SPA navigation. We need to check if it is the case and perform a redirect with the force reload option to ensure the page is loaded through a standard HTTP GET request.

Make the following changes to the `Components/Pages/Login.razor` page:

* Add the `FormName="LoginForm"` attribute to the `<EditForm>` element.
* Decorate the `Model` property with the `[SupplyParameterFromForm]` attribute.
* Update the `OnInitializedAsync` method like this:
    ```csharp
        protected override Task OnInitializedAsync()
        {
            // make sure we are in the server-rendered mode
            if (httpContextAccessor.HttpContext.WebSockets.IsWebSocketRequest)
            {
                navigation.Refresh(forceReload: true);
            }

            // ...
        }
    ```
* Update the `SignIn` method like this:
    ```csharp
    public async Task SignIn()
    {
        try
        {
            // ...

            await apiClient.ValidateCredentialsAsync(Model.UserName, Model.Password);

            var identity = new ClaimsIdentity(
                [ new Claim(ClaimTypes.Name, Model.UserName) ], CookieAuthenticationDefaults.AuthenticationScheme);

            await httpContextAccessor.HttpContext!.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            navigation.NavigateTo("/admin/products");
        }
        // ...
    }
    ```

Next, we need to use a different render mode for the `/login` route. Update `App.razor` like this:

```dothtml
@inject IHttpContextAccessor httpContextAccessor

...

<body>
    <Routes @rendermode="PageRenderMode" />
    <script src="_framework/blazor.web.js"></script>
</body>
</html>

@code
{
    public IComponentRenderMode? PageRenderMode
    {
        get
        {
            if (httpContextAccessor.HttpContext!.Request.Path
                .StartsWithSegments("/login"))
            {
                return null;  // server-rendered mode
            }
            else
            {
                return new InteractiveServerRenderMode(prerender: false);
            }
        }
    }
}
```

And finally, we have to replace the `/logout` endpoint that was implemented in the old ASP.NET app using an empty page. Add the following code to `Program.cs`:

```csproj
// ...

app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(
        CookieAuthenticationDefaults.AuthenticationScheme);
    context.Response.Redirect("/");
});

// ...
```

> Now it is a good idea to commit your changes.

## Caching

In previous sections, we have set up the cache invalidation mechanism based on SignalR Core. Now is the time to remove it. 

It is also a good idea to replace the static access to `HttpRuntime.Cache` with `IMemoryCache` and change the `Utils` class to use instance methods. All consuments of the `Utils` class can request it via dependency injection.

```csharp
public class Utils(ApiClient apiClient, IMemoryCache memoryCache)
{
    public async Task<string> GetProductPriceWithCaching(Guid productId, string selectedCurrency)
    {
        var cacheKey = $"ProductPrice_{selectedCurrency}_{productId}";
        var productPrice = memoryCache.Get(cacheKey) as string;
        if (productPrice == null)
        {
            double? price = null;
            try
            {
                price = await apiClient.GetProductPriceAsync(productId, selectedCurrency);
            }
            catch (ApiException ex) when (ex.StatusCode == 404)
            {
            }
            productPrice = price != null ? $"{price} {selectedCurrency}" : "Price unavailable";
            memoryCache.Set(cacheKey, productPrice);
        }
        return productPrice;
    }

    public void ResetProductPriceWithCache(Guid productId, string selectedCurrency)
    {
        var cacheKey = $"ProductPrice_{selectedCurrency}_{productId}";
        memoryCache.Remove(cacheKey);
    }
}
```

Register the class in `Program.cs`:

```csharp
// ...

builder.Services.AddScoped<Utils>();
builder.Services.AddMemoryCache();

// ...
```

Also, you can delete the `CacheInvalidationHub` and remove its registration.

You will have to update all usages of the `Utils` class:

```dothtml
@inject Utils utils

...

@code {
    // ...
    var price = await utils.GetProductPriceWithCaching(product.Id, await selectedCurrencyService.GetCurrency());
    // ...
}
```

Fix all compile errors.

> Now it is a good time to commit your changes.

## Final cleanup

You can now remove the `AddRemoteClient` call in `Program.cs` and the corresponding configuration keys in `appsettings.json`.

You can also remove YARP.

I recommend keeping the `AddSystemWebAdapters` and `UseSystemWebAdapters` in the project – they might still be necessary as the code base may reference the old `System.Web.HttpContext` objects. 

If you want to remove them as well, you will probably need to add `builder.Services.AddHttpContextAccessor()` as some classes may need it. `SystemWebAdapters` were registering this service for their internal purposes.

> Now it is a good idea to commit your changes.

## Running the app

🎉 Congratulations! You managed to migrate your first project from .NET Framework to the new .NET!

In the real world, you'd now need to figure out how to replace the old deployed application with the new one. This depends on various environment constraints. 

It may be wise to prepare a quick way back if anything goes wrong. It is also a good idea to run load tests for the pages where you expect the most requests, to be sure that the application will not crash because of being significantly slower than the old version.