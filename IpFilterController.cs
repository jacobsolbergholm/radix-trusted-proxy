using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("/")]
public class IpFilterController : ControllerBase
{
    [HttpGet("unfiltered")]
    public IActionResult GetUnfiltered()
    {
        return Ok($"Unfiltered!\n\nRemote IP: {HttpContext.Connection.RemoteIpAddress}");
    }

    [ServiceFilter(typeof(ClientIpCheckActionFilter))]
    [HttpGet("filtered")]
    public IActionResult GetFiltered()
    {
        return Ok($"Filtered!\n\nRemote IP: {HttpContext.Connection.RemoteIpAddress}");
    }
}