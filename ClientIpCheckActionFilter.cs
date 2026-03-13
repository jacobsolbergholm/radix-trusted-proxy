using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class ClientIpCheckActionFilter : IActionFilter
{
    private readonly ILogger<ClientIpCheckActionFilter> _logger;
    private readonly IPAddress[] _safelist;

    public ClientIpCheckActionFilter(ILogger<ClientIpCheckActionFilter> logger, string safelist)
    {
        _logger = logger;
        _safelist = safelist.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(ip => IPAddress.Parse(ip.Trim()))
            .ToArray();
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var remoteIp = context.HttpContext.Connection.RemoteIpAddress;
        _logger.LogDebug("Remote IpAddress: {RemoteIp}", remoteIp);

        if (remoteIp == null)
        {
            context.Result = new StatusCodeResult(403);
            return;
        }

        // Normalize: if the address is IPv4-mapped IPv6 (::ffff:x.x.x.x), convert to IPv4
        var normalizedIp = remoteIp.IsIPv4MappedToIPv6 ? remoteIp.MapToIPv4() : remoteIp;

        if (!_safelist.Any(safe => safe.Equals(normalizedIp) || safe.Equals(remoteIp)))
        {
            _logger.LogWarning("Forbidden Request from IP: {RemoteIp}", remoteIp);
            context.Result = new StatusCodeResult(403);
            return;
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}