using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Configure Forwarded Headers Middleware
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Clear the default known networks/proxies to only trust your specific configuration
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();

    // Add cluster IP range as a trusted proxy
    options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("10.0.0.0"), 8));

    // Specify which headers to use (X-Forwarded-For needed to overwrite RemoteIpAddress)
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
});

builder.Services.AddScoped(container => 
{
    var logger = container.GetRequiredService<ILogger<ClientIpCheckActionFilter>>();
    var safelist = Environment.GetEnvironmentVariable("IP_ALLOW_LIST") ?? string.Empty;

    return new ClientIpCheckActionFilter(logger, safelist);
});

var app = builder.Build();

// Use the Forwarded Headers Middleware at the beginning of the pipeline
app.UseForwardedHeaders();

// ... other middleware (e.g., app.UseRouting(), app.UseAuthorization(), etc.) ...

app.MapControllers();

app.Run();