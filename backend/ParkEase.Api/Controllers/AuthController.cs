using Microsoft.AspNetCore.Mvc;
using ParkEase.Api.DTOs;
using ParkEase.Api.Services;

namespace ParkEase.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Registers a new user (Driver, Operator, or Admin) and returns a JWT access token.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/auth/register
        ///     {
        ///        "fullName": "Sanjeev Veerapandian",
        ///        "email": "sanjeev@parkease.com",
        ///        "password": "Test@1234",
        ///        "phoneNumber": "9876543210",
        ///        "role": "Driver"
        ///     }
        ///
        /// Role must be exactly one of: Driver, Operator, Admin.
        /// </remarks>
        /// <response code="201">User created successfully — returns the JWT token.</response>
        /// <response code="400">Validation failed on the request body.</response>
        /// <response code="409">Email already registered, or an invalid role was specified.</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterAsync(dto);

            if (result == null)
            {
                return Conflict(new { message = "Email already registered, or invalid role specified." });
            }

            return CreatedAtAction(nameof(Register), result);
        }

        /// <summary>
        /// Authenticates an existing user and returns a fresh JWT access token.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/auth/login
        ///     {
        ///        "email": "sanjeev@parkease.com",
        ///        "password": "Test@1234"
        ///     }
        ///
        /// Use the returned token in the Authorize dialog as: Bearer &lt;token&gt;
        /// </remarks>
        /// <response code="200">Login succeeded — returns the JWT token.</response>
        /// <response code="400">Validation failed on the request body.</response>
        /// <response code="401">Invalid email or password.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(dto);

            if (result == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            return Ok(result);
        }
    }
}