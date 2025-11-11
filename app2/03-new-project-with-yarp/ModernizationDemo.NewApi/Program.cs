var builder = WebApplication.CreateBuilder(args);

// register YARP services
builder.Services.AddHttpForwarder();

var app = builder.Build();

// proxy all requests toe the old application
app.MapForwarder("/{**catch-all}", app.Configuration["ProxyTo"])
	.Add(static builder => ((RouteEndpointBuilder)builder).Order = int.MaxValue);

app.Run();
