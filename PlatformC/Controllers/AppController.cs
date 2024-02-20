using Microsoft.AspNetCore.Mvc;

namespace PlatformB.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AppController : ControllerBase
    {
        [HttpGet()]
        public IActionResult Get()
        {
            return Ok();
        }
    }
}