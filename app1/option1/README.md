# ASP.NET Web Forms In-Place Modernization Example

Welcome to the **ASP.NET Web Forms In-Place Modernization Example** app. 

This application contains several ASP.NET Web Forms pages and uses various mechanism, such as authentication, session or caching.

To make it easier, most of the business logic was abstracted away to a separate microservice that exposes a REST API.

The app is built using **.NET Framework 4.7.2**. 

Our goal is to use in-place migration while rewriting all pages to [DotVVM](https://www.dotvvm.com).

## Install Prerequisites

Make sure you have the following software installed:

* Visual Studio 2022 with ASP.NET workload and .NET Framework SDK for version 4.7.2
* Microsoft SQL Server Management Studio (or some other tool to work with SQL databases)

Since we'll be using **DotVVM**, you'll also need to install the **DotVVM for Visual Studio** extension.

* Make sure you have updated to the latest version of Visual Studio 2022.
* Open the **Extension Manager** window in Visual Studio and search for **dotvvm**.
* Install the extension. A restart of Visual Studio will be needed.

> The extension for the RTM version of Visual Studio 2026 hasn't been released yet. 

## Run SQL Server Management Studio and ensure connection to the SQL Server

Check whether you have SQL Server LocalDB (it should be installed with Visual Studio) or SQL Server Express. 

* For SQL Server LocalDB, the server name should be `(localdb)\MSSQLLocalDB`

* SQL Server Express is usually at `.\SQLEXPRESS`

Run SQL Server Management Studio and make sure you can connect to the database server.

## Clone the repository

If you haven't done this yet, clone the [GitHub repository](https://github.com/tomasherceg/modernization-workshop) to some folder on your disk.

Navigate to `app1/option1` folder.

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

## Add DotVVM in the project

Since we will use the in-place migration approach, we first need to install `DotVVM` in the project. The unique feature of DotVVM is that is runs on both .NET Framework and the new .NET. 

Our goal will be to replace all Web Forms pages with their DotVVM equivalents (while the project is still on .NET Framework), and once we are done with that, we'll be able to switch the target framework to .NET 8 or beyond.

In Visual Studio, right-click on the `ModernizationDemo.App` project and use the **Add DotVVM** option.

This will do a couple of steps:

* Install `DotVVM.Owin` NuGet package.
* Install `Microsoft.Owin.Host.SystemWeb` package.
* Add the `Startup.cs` and `DotvvmStartup.cs` files with the configuration of DotVVM.
* Created the `Views` and `ViewModels` folders.
* Add a value in the `<ProjectTypeGuids>` element in the `csproj` file.

> Visual Studio needs to reload the project, and some other project is changed to be the startup project. Right-click on the `ModernizationDemo.App` project and select the **Set as startup project** again.

Compile and run the project to make sure nothing got broken.

> You can look in the `02-add-dotvvm` folder to see the example of how the project should look like.

## Install DotVVM.Adapters.WebForms package

We'll need one more NuGet package that can simplify the modernization process. During the migration, we'll be having two types of routes in our app - ASP.NET Web Forms routes and DotVVM routes - and we will need to transition between the old and new parts of the app. This package contains several components and extension methods to handle these scenarios.

Install the `DotVVM.Adapters.WebForms` NuGet package. Make sure the version number is the same as the version of `DotVVM` package - it should be `4.3.7`.

Then, add the following registration in the `Configure` method in `DotvvmStartup.cs`:

```csharp
config.AddWebFormsAdapters();
```

## Enable the new C# syntax in the project

Because we want to use new C# syntax features such as collection expressions, we will need to manually edit the project file and add <LangVersion>12</LangVersion> in it. 

* Unload the `ModernizationDemo.App` project.

* Right-click on it and select **Edit project file**.

* Add the `<LangVersion>12</LangVersion>` element in the first `<PropertyGroup>`.

* Reload the project and set it as startup project.

> Now it is a good idea to commit your changes.

## Migrate the master page

Because all Web Forms pages are embedded in a master page, we need to start by migrating the master page into DotVVM.

Right-click on the `Views` folder and select **Add Item**. If you use the compact view, click on the **Show All Templates** button.

In the list, select **DotVVM Master Page**, and name it `Site.dotmaster`. In the next window, continue with the defaults - the extension will create the viewmodel for the master page. 

## Using ASPX to DotVVM converter

To simplify migration from ASP.NET Web Forms syntax to DotVVM, you can use [ASPX to DotVVM converter](https://www.dotvvm.com/webforms/convert).

Open the page and paste the contents of the `Pages/Site.master` (**except for the first line**) in the text area. Then, click on the **Continue** button.

The converter will inspect the code and suggest some fixes - you can review and apply them using the links on the right side. 

> The converter doesn't cover all differences between Web Forms and DotVVM, only the most frequent cases are covered. If you find any scenarios that the converter doesn't support, you can file an issue or submit a PR in its [GitHub repo](https://github.com/riganti/dotvvm-webforms).

Apply all suggestions and copy the converter output to clipboard.

Then, open `Views/Site.dotmaster` file, **keep only the first line**, and paste the clipboard content.

## Replacing the LoginStatus control

The converter didn't know what to do with the `<asp:LoginStatus>` component. This component can render the **Sign In** and **Sign Out** links based on whether the user is authenticated or not.

Replace this control with the following code snippet:

```dothtml
<dot:AuthenticatedView>
    <NotAuthenticatedTemplate>
        <webforms:HybridRouteLink RouteName="Login" 
                                  Text="Sign in" 
                                  class="nav-link" />
    </NotAuthenticatedTemplate>
    <AuthenticatedTemplate>
        <dot:LinkButton Text="Sign out" 
                        Click="{command: SignOut()}"
                        Validation.Enabled="false" 
                        class="nav-link" />
    </AuthenticatedTemplate>
</dot:AuthenticatedView>
```

When the user is not signed in, they will see a link to the `Login` route (we are using the Web Forms adapters `HybridRouteLink` that will look whether the route is defined in Web Forms or in DotVVM and use the appropriate one). For authenticated users, a button calling the `SignOut` method in the viewmodel will be rendered. 

Then, open the `ViewModels/SiteViewModel.cs` file and add the following method:

```csharp
public void SignOut()
{
    FormsAuthentication.SignOut();
    Context.RedirectToRouteHybrid("Products");
}
```

Again, we are using the `RedirectToRouteHybrid` from the Web Forms Adapters package to make it work with both DotVVM and Web Forms route (since we don't know whether the page was already migrated or not).

## Replacing the DropDownList control

Another component in the master page that we need to change is `<asp:DropDownList>`. DotVVM has a similar component called `ComboBox`, but its usage is different. The list of its options must be in the viewmodel, and the data-binding is used to interact with the component.

Replace the `<asp:DropDownList>` in `Views/Site.dotmaster` with the following code snippet:

```dothtml
<dot:ComboBox class="form-select me-2"
              Validation.Enabled="false"
              SelectedValue="{value: SelectedCurrency}"
              DataSource="{value: Currencies}"
              SelectionChanged="{command: OnCurrencyChanged()}" />
```

We will need to declare the following things in `SiteViewModel.cs`:

```csharp
[Bind(Direction.ServerToClientFirstRequest)]
public List<string> Currencies { get; set; } = [ "USD", "EUR", "JPY", "GBP" ];

public string SelectedCurrency { get; set; } = "USD";

public override Task Init()
{
    if (HttpContext.Current.Session["SelectedCurrency"] is string currency)
    {
        SelectedCurrency = currency;
    }
    return base.Init();
}

public void OnCurrencyChanged()
{
    HttpContext.Current.Session["SelectedCurrency"] = SelectedCurrency;
    Context.RedirectToLocalUrl(Context.HttpContext.Request.Url.PathAndQuery);
} 
```

Notice the `[Bind]` attribute that decorates the `Currencies` property. Because the list of currencies never changes, this attribute tells DotVVM that it needs to be transferred only when the page is loaded the first time (using HTTP GET request) and does not need to be transferred to the server on postbacks. This helps to reduce the amount of data exchanged between the server and the client.

We don't use the `PageBase` class (a base class for all Web Forms pages) to access the `SelectedCurrency` property which used session in its getter and setter. Instead, we reimplemented this functionality in `SiteViewModel` because it will now be the base class for all viewmodels. Also, because the viewmodel gets JSON-serialized, it is not recommended to place anything in the getters and setters, as there is no guarantee in which order the properties will be accessed by the serializer. That is why we explicitly obtain the value from the session in the Init phase and set it directly when the user selects another currency in the `ComboBox`.

## Replacing the link to RSS feed

The last unsupported bit is the `<%$ RouteUrl: ... %>` expression builder that Web Forms used to build route URLs. We can just migrate it to `HybridRouteLink` like so:

```dothtml
<webforms:HybridRouteLink RouteName="ProductsRss" Text="RSS" />
```

## Testing the master page

Let's add the first DotVVM page which will use the new master page. Right-click on the `Views` folder, select **Add New Item**, and create **DotVVM Page** named `ProductDetail.dothtml`.

In the next window, ensure that the page will be embedded in our new master page, and confirm the selection.

We need to register the new page in DotVVM route table to be handled. Open `DotvvmStartup.cs` and change the `ConfigureRoutes` method like this:

```csharp
private void ConfigureRoutes(DotvvmConfiguration config, string applicationPath)
{
    // register routes   
    config.RouteTable.Add("ProductDetail", "product/{id}", "Views/ProductDetail.dothtml");
}
```

This will register the new page under the root URL of the website, replacing the Web Forms `ProductDetail.aspx` page.

If you run the website and select any item on the homepage, you will see just the header and footer with no content, because we didn't place any content in our new page. But it is already rendered by DotVVM - you can right-click on the page, view the source HTML and see there is no `__VIEWSTATE` hidden field.

> If you get an error saying "The viewmodel ProductDetailViewModel is not assignable to the viewmodel of the master page SiteViewModel.", open the `ProductDetailViewModel.cs` and change the base class to `SiteViewModel`. The DotVVM extension sometimes doesn't figure out the base class correctly.

> If you run in trouble, refer to the `03-master-page-and-first-page` folder where the master page and the product detail pages are migrated.

> Now it is a good idea to commit your changes.

## Use converter for the ProductDetail page

We can now use the same approach as with the master page:

* Paste the contents of `Pages/ProductDetail.aspx` (**except for the first line**) in [ASPX to DotVVM converter](https://www.dotvvm.com/webforms/convert).
* Apply all suggestions.
* Paste the result back to `Views/ProductDetail.dothtml` (be sure to **keep the first lines** starting with `@`).

## Fix the FormView on ProductDetail page

The first issue is that `<asp:FormView>` control is not supported. You can change it to `<div DataContext="{value: Product}">` and remove its inner `<ItemTemplate>` element.

The `DataContext` binding tells DotVVM to evaluate all data-binding expressions inside the `<div>` element in the context of the `Product` property of type `ProductModel` in viewmodel – therefore, we will be able to use just `{value: Title}` instead of `{value: Product.Title}`.

In the `ProductDetailViewModel`, we will need to declare the `Product` property, and load its value using the API client when the page is loaded. If you compare it with the code-behind of the `ProductDetail.aspx` page, you will see that it's not that different - we just use the MVVM approach instead of interacting directly with UI controls in the page. The `FromRoute` attribute tells DotVVM to get the product ID from the route parameter named `Id`.

```csharp
public class ProductDetailViewModel(ApiClient apiClient) : SiteViewModel
{
    [FromRoute("Id")]
    public Guid Id { get; set; }

    public ProductModel Product { get; set; }


    public override async Task PreRender()
    {
        if (!Context.IsPostBack)
        {
            Product = await apiClient.GetProductAsync(Id); 
        }
        await base.PreRender();
    }
}
```

## Fix the price literal

To fix the `<asp:Literal>` control in the page, we will declare the `Price` property in our viewmodel, and just write it out. Because the `Price` property will be outside the `Product` model, we need to use the `_root`:

```dothtml
<!-- replace the <asp:Literal> control with this: -->
{{value: _root.Price}}
```

```csharp
//...

public string Price { get; set; }

public override async Task PreRender()
{
    if (!Context.IsPostBack)
    {
        // ...
        Price = Utils.GetProductPriceWithCaching(Id, SelectedCurrency);
    }
    // ...
}
```

## Registering the API client

Because we started using dependency injection, we need to register the backend API client in the `IServiceCollection`. This wasn't necessary in Web Forms as there was no built-in mechanism for DI, and the instance of the client was created manually in the code-behind using a factory method from the `Global.asax` class.

Add the following line in the `ConfigureServices` method in `DotvvmStartup.cs`:

```csharp
public void ConfigureServices(IDotvvmServiceCollection options)
{
    // ...
    options.Services.AddScoped(_ => Global.GetApiClient());
}
```

> If you run in trouble, refer to the `03-master-page-and-first-page` folder where the master page and the product detail pages are migrated.

> Now it is a good idea to commit your changes.

## Migrate other pages

We'll now use the same approach for the remaining pages:

* `Pages/Default.aspx`
* `Pages/Login.aspx`
* `Pages/Admin/Products.aspx`
* `Pages/Admin/ProductDetail.aspx`

The scheme is always the same:

* Paste the contents of the Web Forms page (**except for the first line**) in [ASPX to DotVVM converter](https://www.dotvvm.com/webforms/convert).
* Apply all suggestions.
* Create the DotVVM page in the `Views` folder and paste the result back to the `.dothtml` file, **preserving the first lines** starting with `@`.
* Register the page route in `DotvvmStartup.cs`, overriding the Web Forms page.
* Fix the incompatibilities that the converter couldn't do itself.

Let's take a shortcut here:

* Copy the `Views`, `Controls` and `ViewModels` folders from `04-all-pages-and-handler` folder to your project.
* Use the following definition of `ConfigureRoutes` method for `DotvvmStartup.cs` to ensure all DotVVM routes are registered:
    ```csharp
    private void ConfigureRoutes(DotvvmConfiguration config, string applicationPath)
    {
        // register routes   
        config.RouteTable.Add("Login", "login", "Views/Login.dothtml");

        config.RouteTable.Add("Products", "", "Views/Default.dothtml");
        config.RouteTable.Add("ProductDetail", "product/{id}", "Views/ProductDetail.dothtml");

        config.RouteTable.Add("AdminProducts", "admin/products", "Views/Admin/Products.dothtml");
        config.RouteTable.Add("AdminProductCreate", "admin/product", "Views/Admin/ProductDetail.dothtml");
        config.RouteTable.Add("AdminProductDetail", "admin/product/{id}", "Views/Admin/ProductDetail.dothtml");
    } 
    ```
* Register the custom `DataPager` control in `ConfigureControls` in `DotvvmStartup.cs`:
    ```csharp
    private void ConfigureControls(DotvvmConfiguration config, string applicationPath)
    {
        config.Markup.AddMarkupControl("cc", "CustomDataPager", "Controls/CustomDataPager.dotcontrol");
    }
    ```

> If you run in trouble, refer to the `04-all-pages-and-handler` folder where the master page and the product detail pages are migrated.

## Optional reading: Differences between DotVVM and Web Forms

There are quite a lot of differences between ASP.NET Web Forms and DotVVM:

* The home page uses paging on the list of products, which is done by specifying `startRowIndex`, `maximumRows`, and `totalRowCount` parameters in the `GetData` method used for model binding. DotVVM uses another approach – instead of using `List<T>` in the viewmodel, you can use `GridViewDataSet<T>`, which is basically `List<T>` accompanied with metadata for sorting and paging. For example, there is the PagingOptions property holding the index of the current page, page size, and the total number of records in the data set. Data sets in DotVVM can be loaded from `IQueryable` by a useful `LoadFromQueryable` helper method, or you can fill their contents manually. In our case, I assigned the results to the `Items` collection and set the `PagingOptions.TotalRecordsCount` property manually, because I am not able to obtain `IQueryable` from the API.
* DotVVM has a component called `DataPager`, which has a similar usage as the Web Forms `DataPager`. However, in the current version of DotVVM, it is not customizable enough to be able to render just the previous and next buttons as we do in Web Forms. However, the `GridViewDataSet` contains all the properties required for building the experience ourselves. In the example application, you can find a `<cc:CustomDataPager>` component that renders a similar UI as the Web Forms `DataPager`. Notice it declares one property of type `IPageableGridViewDataSet` (without the generic parameter), which makes it pretty universal – it will work with a data set of anything.
* DotVVM does not have the `<asp:Login>` component. However, it is quite easy to define the form yourself. Because the project still runs on .NET Framework, we can continue signing the user in by calling `FormsAuthentication.SetAuthCookie`. This will have to be changed in the final step when we switch the project to ASP.NET Core.
* Notice the Data Annotations attributes and the `ValidationSummary` components in `Views/Login.dothtml` used to perform validation. It is easier than using `RequiredFieldValidator` controls in ASP.NET Web Forms. Also, you do not have to manually check `Page.IsValid` in commands. DotVVM will not invoke the command when the viewmodel does not pass validation.
* Redirecting to other pages can be done by using `Context.RedirectToRoute`. Since we can suffer from the same issue as with the `RouteLink` control (some routes are not yet present in the DotVVM routing table), you can use the `RedirectToRouteHybrid` extension method, which is also added by the `DotVVM.Adapters.WebForms` NuGet package.
* The `Views/Admin/Products.dothtml` page uses `GridView`, and like the home page, it is easy to use `GridViewDataSet` to handle sorting and paging. To delete a product, you can call a command and give it parameters from the current data context. Notice the command binding `{command: _root.DeleteProduct(Id)}`, which calls the `DeleteProduct` method declared on the page viewmodel, and gives it the current record's `Id`. After deleting the product, you need to call `Products.RequestRefresh()` to indicate the data set shall be reloaded. In ASP.NET Web Forms, the delete method was invoked by the `GridView` control itself, and because of that, it knew that the data had to be reloaded afterward. In our case, the deletion is invoked by an arbitrary button, which is not aware of the situation. Notice that we load the data in the `PreRender` method only when `Products.IsRefreshRequested`. This pattern helps to make the page more efficient by not reloading the data every time, for example, in postbacks that do not manipulate the data set.
* As was already mentioned, the validation in DotVVM works differently than in Web Forms. Instead of validating controls, which was the case in Web Forms (the validator controls had a property named `ControlToValidate`), DotVVM validates the viewmodel (or its part). To define the validation rules, you can use either Data Annotation attributes on viewmodel properties or implement the `IValidatableObject` interface to perform the validation in code. Additionally, you can add custom errors into the model state by calling `viewModel.AddModelError(vm => vm.Property, message)`. Validation controls in DotVVM only display the errors from the `ModelState` object – for instance, by showing the error message, applying a CSS class to a particular element, and so on.
* Instead of validation groups (groups of controls that were validated together), DotVVM uses the viewmodel hierarchy. If you bind the control's `Validation.Target` property to a particular object in the viewmodel, you tell DotVVM that any postback coming from that control will validate only the specific object instead of validating the entire viewmodel. You can also turn the validation off completely by setting `Validation.Enabled=false` on a control that makes postbacks. In `Views/Admin/ProductDetail.dothtml` page, you can see that the insert row for the new prices has the validation target set to the `NewPrice` object, but the main **Save** button for the entire page sets the target to the `Product` object. If you add the new price for the product, the product itself may not be valid at that moment. Similarly, the new price row does not have to be valid when saving the product.
* The admin area is using model objects from the `ModernizationDemo.BackendClient` project generated by NSwag. Because we need to use these models in editable forms, we need to decorate some of the properties with validation attributes, such as `[Required]`. The model classes are declared as `partial`, but the properties are declared in the other part of the class, and thus, it is not possible to attach attributes on them. To solve this issue, we can declare an inner metadata class with the same properties, and use the attributes there:
    ```csharp
    public partial class ProductCreateEditModel
    {
        class Metadata
        {
            [Required(ErrorMessage = "Product name is required!")]
            public string Name { get; set; }

            [Required(ErrorMessage = "Product description is required!")]
            public string Description { get; set; }

            [Required(ErrorMessage = "Product image is required!")]
            public string ImageUrl { get; set; }
        }
    }
    ```
    Then, you need to tell DotVVM about these attributes by implementing the `IViewModelValidationMetadataProvider` interface and registering it for the dependency injection. You can refer to the `Extensions/CustomViewModelValidationMetadataProvider.cs` class to see how it is done.

## Replacing HTTP handler that generates the RSS feed

The last dependency on Web Forms is the `ProductsRssHandler` that is responsible for generating the site's RSS feed. 

DotVVM has a similar concept of **presenters**. Create a new class called `ProductRssPresenter` and implement the `IDotvvmPresenter` interface. There are just a few rather cosmetic changes:

```csharp
public Task ProcessRequest(IDotvvmRequestContext context)
{
    context.HttpContext.Response.ContentType = "application/atom+xml";
    context.HttpContext.Response.Headers["Cache-Control"] = "private";
    context.HttpContext.Response.Headers["Expires"] = DateTime.UtcNow.AddMonths(1).ToString("R");

    var baseUri = GetApplicationBaseUri(context.HttpContext);

    // ...

    using (var xw = new System.Xml.XmlTextWriter(context.HttpContext.Response.Body, Encoding.UTF8))

    // ...

    return Task.CompletedTask;
}

// ...

public static Uri GetApplicationBaseUri(IHttpContext context)
{
    var uriBuilder = new UriBuilder(context.Request.Url);
    uriBuilder.Path = context.Request.PathBase.Value ?? string.Empty;
    
    // ...
}
```

We will need to register the presenter in the `ConfigureRoutes` and `ConfigureServices` methods `DotvvmStartup.cs`:

```csharp
private void ConfigureRoutes(DotvvmConfiguration config, string applicationPath)
{
    // ...

    config.RouteTable.Add("ProductsRss", "products/rss", typeof(ProductsRssPresenter));
} 

// ...

public void ConfigureServices(IDotvvmServiceCollection options)
{
    // ...

    options.Services.AddScoped<ProductsRssPresenter>();
}
```

> If you run in trouble, refer to the `04-all-pages-and-handler` folder where the master page and the product detail pages are migrated.

> Now it is a good idea to commit your changes.

## Cleaning up before switching to .NET 8

We have successfully reached the point where all business logic is accessed through DotVVM pages or the DotVVM presenter. Now, it is time to remove all ASP.NET Web Forms artifacts and all other dependencies that prevent us from switching the target framework to the new .NET.

Delete the following items:

* The entire `Pages` folder
* `Global.asax`
* `packages.config`
* `PageBase.cs`
* `GenericRouteHandler.cs`. 
* `Handlers/ProductsRssHandler.cs`
* `web.config` (including its `Debug` and `Release` versions) 
* `Properties/AssemblyInfo.cs`

Next, create the `wwwroot` and move the `Scripts` and `Styles` folders in this folder.

## Unload the project and migrate the file to SDK-style project

Right-click on the `ModernizationDemo.App` project and select **Unload project**.

Double-click on the project name to show the contents of the project file, and replace the contents with the new definition:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Content Include="**/*.dothtml;**/*.dotmaster;**/*.dotcontrol" Exclude="obj/**/*.*;bin/**/*.*" CopyToPublishDirectory="Always" />
    <None Remove="**/*.dothtml;**/*.dotmaster;**/*.dotcontrol" />
  </ItemGroup>
  <ItemGroup>
    <None Remove="dotvvm_serialized_config.json.tmp" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="DotVVM.Adapters.WebForms" Version="4.3.7" />
    <PackageReference Include="DotVVM.AspNetCore" Version="4.3.7" />
    <PackageReference Include="System.ServiceModel.Syndication" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ModernizationDemo.BackendClient\ModernizationDemo.BackendClient.csproj" />
  </ItemGroup>
</Project>
```

As you can see, the new project file is very simple. It targets .NET 8, references only two NuGet packages (`DotVVM.AspNetCore` and `DotVVM.Adapters.WebForms`), and contains conventions for treating DotVVM-specific files. I believe it is better to start with a minimalistic project file and add only the things that are necessary, than trying to adopt all references from the previous version.

Save the changes and right-click on the project and select **Reload project**.

After that, right-click on the project again and use the **Set as startup project**.

## Change Startup.cs to Program.cs

We need to create an entry-point for our application. 

Rename the `Startup.cs` file to `Program.cs`, and replace its contents to this:

```csproj
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDotVVM<DotvvmStartup>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseDotVVM<DotvvmStartup>();
app.UseStaticFiles();

app.Run();
```

## Authentication and authorization

Our application was using **Form authentication** and **web.config authorization rules**, which are not available on the new .NET. We want to use **ASP.NET Core Cookie authentication**.

First, replace the call to `FormsAuthentication.SetAuthCookie` in `LoginViewModel.cs` with this:

```csharp
// ...
var identity = new ClaimsIdentity(
    [new Claim(ClaimTypes.Name, UserName)], CookieAuthenticationDefaults.AuthenticationScheme);
await Context.GetAuthentication().SignInAsync(
    CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
// ...
```

Then, update the `SignOut` method in `SiteViewModel.cs` to this:

```csharp
public async Task SignOut()
{
    await Context.GetAuthentication()
        .SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    Context.RedirectToRouteHybrid("Products");
}
```

Remove the usings of `System.Web.Security` in both files.

Also, override the `Init` method in `Admin/ProductsViewModel.cs` and `Admin/ProductDetailViewModel.cs`:

```csproj
public override async Task Init()
{
    await Context.Authorize();
    await base.Init();
}
```

Finally, the cookie authentication needs to be registered in `Program.cs` like so:

```csharp
builder.Services
	.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme,
		options =>
		{
			options.Events = new CookieAuthenticationEvents
			{
				OnRedirectToReturnUrl = c =>
					DotvvmAuthenticationHelper.ApplyRedirectResponse(c.HttpContext, c.RedirectUri),
				OnRedirectToAccessDenied = c =>
					DotvvmAuthenticationHelper.ApplyStatusCodeResponse(c.HttpContext, 403),
				OnRedirectToLogin = c =>
					DotvvmAuthenticationHelper.ApplyRedirectResponse(c.HttpContext, c.RedirectUri),
				OnRedirectToLogout = c =>
					DotvvmAuthenticationHelper.ApplyRedirectResponse(c.HttpContext, c.RedirectUri)
			};

			options.LoginPath = "/login";
		});
```

## Replacing the session

One of the last things we need to get rid of is using the old ASP.NET Session State. Using the session is generally discouraged, but the reality is that even today, it is still a popular concept.

ASP.NET Core supports session, albeit with several limitations.

First, register the required services in `Program.cs`:

```csharp
// ...

builder.Services.AddSession();
builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

app.UseSession();

// ...
```

Then, you can fix references to session in `SiteViewModel`:

```csproj
public override async Task Init()
{
    var session = Context.GetAspNetCoreContext().Session;
    await session.LoadAsync();

    if (session.TryGetValue("SelectedCurrency", out var currency))
    {
        SelectedCurrency = Encoding.ASCII.GetString(currency);
    }
}

public async Task OnCurrencyChanged()
{
    var session = Context.GetAspNetCoreContext().Session;
    await session.LoadAsync();

    session.Set("SelectedCurrency", Encoding.ASCII.GetBytes(SelectedCurrency));
    await session.CommitAsync();
    
    // ...
}
```

## Migrating the cache

The `Utils` class was using `HttpRuntime.Cache`, which was also redesigned on the new .NET 8.

Since we have dependency injection and the ASP.NET Core `IMemoryCache` can be served from there, there is no need for the `Utils` class to be static. Actually, the code will be easier to cover with tests if it isn't.

You can rewrite it as such:

```csproj
public class Utils(IMemoryCache memoryCache, ApiClient apiClient)
{
	public async Task<string> GetProductPriceWithCaching(Guid productId, string selectedCurrency)
	{
		var cacheKey = $"ProductPrice_{selectedCurrency}_{productId}";
		if (!memoryCache.TryGetValue(cacheKey, out var productPrice))
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

		return (string)productPrice;
	}

	public void ResetProductPriceWithCache(Guid productId, string selectedCurrency)
	{
		var cacheKey = $"ProductPrice_{selectedCurrency}_{productId}";
		memoryCache.Remove(cacheKey);
	}
}
```

You will need to update the code on all 4 usages - the `GetProductPriceWithCaching` is now asynchronous, and you must invoke the methods on an instance. The easiest way is to register the `Utils` class in the dependency injection in `Program.cs`, and request it via constructor parameter:

```csharp
// ...

builder.Services.AddScoped<Utils>();

// ...
```

Then, you can just update the call sites:

```csharp
public class ProductsViewModel(ApiClient apiClient, Utils utils) : SiteViewModel
{
    // ...

    public override async Task PreRender()
    {
        // ...
            Prices = new Dictionary<Guid, string>();
            foreach (var result in response.Results)
            {
                Prices[result.Id] = await utils.GetProductPriceWithCaching(result.Id, SelectedCurrency);
            }
        // ...
    }
    // ...
}
```

## Removing usages of Global.GetApiClient

The last two compile errors are caused by the removal of `Global.asax` file.

* In `ProductsRssPresenter`, request the API client using dependency injection.

* The second usage is in `DotvvmStartup`. In the old .NET Framework, this was the only place where you could add stuff into `IServiceCollection`, as there was no `Program.cs`. Remove the line and add the following registration in `Program.cs` instead:

    ```csharp
    // ...
    builder.Services.AddSingleton(_ => new ApiClient(builder.Configuration["Api:Url"], new HttpClient()));
    // ...
    ```

This will require the configuration JSON file we don't have in the project yet. Add the `appsettings.json` file with the following content:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Api": {
    "Url": "https://localhost:7211/"
  }
}
```

This was the last bit that will tell the ASP.NET Core app where the backend API is hosted. This value was previously in `web.config`, but we deleted the file.

> Now it is a good idea to commit your changes.

## Running the app

🎉 Congratulations! You managed to migrate your first project from .NET Framework to the new .NET!

In the real world, you'd now need to figure out how to replace the old deployed application with the new one. This depends on various environment constraints. 

It may be wise to prepare a quick way back if anything goes wrong. It is also a good idea to run load tests for the pages where you expect the most requests, to be sure that the application will not crash because of being significantly slower than the old version.