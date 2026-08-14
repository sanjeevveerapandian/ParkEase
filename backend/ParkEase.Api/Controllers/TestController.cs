using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ParkEase.Api.Controllers
{
    [ApiController]
    [Route("api/test")]
    public class TestController : ControllerBase
    {
        [HttpGet("public")]
        public IActionResult PublicEndpoint()
        {
            return Ok(new { message = "Anyone can see this — no token needed." });
        }

        [Authorize]
        [HttpGet("any-logged-in-user")]
        public IActionResult AnyAuthenticatedUser()
        {
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            return Ok(new { message = $"Hello {email}, you're authenticated (any role)." });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin-only")]
        public IActionResult AdminOnlyEndpoint()
        {
            return Ok(new { message = "You're an Admin. Only Admins can see this." });
        }

        [Authorize(Roles = "Operator")]
        [HttpGet("operator-only")]
        public IActionResult OperatorOnlyEndpoint()
        {
            return Ok(new { message = "You're an Operator. Only Operators can see this." });
        }
    }
}