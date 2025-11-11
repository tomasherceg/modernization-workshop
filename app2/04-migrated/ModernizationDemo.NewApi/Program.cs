using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModernizationDemo.NewApi.Security;
using ModernizationDemo.NewCore;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// API
builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.PropertyNamingPolicy = null;
		options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
	});

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

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.AddSecurityDefinition("apiKey", new OpenApiSecurityScheme()
	{
		Type = SecuritySchemeType.ApiKey,
		In = ParameterLocation.Header,
		Name = "X-ApiKey"
	});
	options.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "apiKey" }
			},
			[]
		}
	});
});

// EF Core
builder.Services.AddDbContext<ShopEntities>(options =>
{
	options.UseSqlServer(builder.Configuration.GetConnectionString("ShopEntities"));
});

// Authentication
builder.Services.AddAuthentication()
	.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationDefaults.AuthenticationScheme, null);

// YARP
builder.Services.AddHttpForwarder();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapForwarder("/{**catch-all}", app.Configuration["ProxyTo"])
	.Add(static builder => ((RouteEndpointBuilder)builder).Order = int.MaxValue);

app.Run();
