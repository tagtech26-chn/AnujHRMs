using Microsoft.AspNetCore.Mvc;

namespace Attendance.API.Controllers;

[ApiController]
[Route("api/system")]
public sealed class SystemController : ControllerBase
{
    [HttpGet("info")]
    public IActionResult Info() => Ok(new
    {
        application = "AnujHRMS",
        version = "0.1.0",
        api = "Attendance.API",
        status = "foundation"
    });
}
