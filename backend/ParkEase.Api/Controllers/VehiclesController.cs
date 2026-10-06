using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkEase.Api.Authorization;
using ParkEase.Api.DTOs;
using ParkEase.Api.Services;

namespace ParkEase.Api.Controllers
{
    /// <summary>
    /// A driver's registered vehicles. A booking maps one of these to a slot of the same type.
    /// </summary>
    [ApiController]
    [Route("api/vehicles")]
    [Authorize(Policy = AuthPolicies.DriverOnly)]
    public class VehiclesController : ApiControllerBase
    {
        private readonly IVehicleService _service;

        public VehiclesController(IVehicleService service)
        {
            _service = service;
        }

        /// <summary>Lists the calling driver's vehicles.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<VehicleDto>), 200)]
        public async Task<IActionResult> GetMine()
        {
            return Ok(await _service.GetMineAsync(GetUserId()));
        }

        /// <summary>Registers a vehicle (plate is normalized, e.g. "tn 09 ab 1234" → TN09AB1234).</summary>
        /// <response code="201">Vehicle added.</response>
        /// <response code="400">Invalid plate or type.</response>
        /// <response code="409">Already in the driver's vehicles.</response>
        [HttpPost]
        [ProducesResponseType(typeof(VehicleDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Add([FromBody] CreateVehicleDto dto)
        {
            var result = await _service.AddAsync(dto, GetUserId());
            if (!result.Succeeded) return ErrorResult(result);
            return CreatedAtAction(nameof(GetMine), result.Value);
        }

        /// <summary>Removes a vehicle. Vehicles with booking history are archived rather than deleted.</summary>
        /// <response code="204">Removed.</response>
        /// <response code="409">The vehicle has a live booking.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Remove(int id)
        {
            var result = await _service.RemoveAsync(id, GetUserId());
            return result.Succeeded ? NoContent() : ErrorResult(result);
        }
    }
}
