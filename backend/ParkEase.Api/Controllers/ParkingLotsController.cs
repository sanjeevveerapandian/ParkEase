using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkEase.Api.DTOs;
using ParkEase.Api.Services;

namespace ParkEase.Api.Controllers
{
    [ApiController]
    [Route("api/parkinglots")]
    [Authorize(Roles = "Admin,Operator")]
    public class ParkingLotsController : ControllerBase
    {
        private readonly IParkingLotService _service;

        public ParkingLotsController(IParkingLotService service)
        {
            _service = service;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);

        private bool IsAdmin() => User.IsInRole("Admin");

        /// <summary>
        /// Lists parking lots. Admins see every lot; Operators see only lots they own.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ParkingLotDto>), 200)]
        public async Task<IActionResult> GetLots()
        {
            var lots = await _service.GetLotsAsync(GetUserId(), IsAdmin());
            return Ok(lots);
        }

        /// <summary>
        /// Gets a single lot by id, subject to the same ownership scoping as the list endpoint.
        /// </summary>
        /// <response code="200">Lot found and accessible to the caller.</response>
        /// <response code="404">Lot not found, or not owned by the caller.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ParkingLotDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetLot(int id)
        {
            var lot = await _service.GetLotByIdAsync(id, GetUserId(), IsAdmin());
            if (lot == null) return NotFound();
            return Ok(lot);
        }

        /// <summary>
        /// Creates a new parking lot, owned by the calling Operator. Operator-only.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/parkinglots
        ///     {
        ///        "name": "Anna Nagar Lot 1",
        ///        "address": "12 2nd Ave, Anna Nagar, Chennai"
        ///     }
        ///
        /// The owning Operator is taken from the JWT token, not the request body.
        /// </remarks>
        /// <response code="201">Lot created.</response>
        [HttpPost]
        [Authorize(Roles = "Operator")]
        [ProducesResponseType(typeof(ParkingLotDto), 201)]
        public async Task<IActionResult> CreateLot([FromBody] CreateParkingLotDto dto)
        {
            var lot = await _service.CreateLotAsync(dto, GetUserId());
            return CreatedAtAction(nameof(GetLot), new { id = lot.Id }, lot);
        }

        /// <summary>
        /// Deletes a lot. Admin can delete any lot; Operators can only delete their own.
        /// </summary>
        /// <response code="204">Lot deleted.</response>
        /// <response code="404">Lot not found, or not owned by the caller.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteLot(int id)
        {
            var success = await _service.DeleteLotAsync(id, GetUserId(), IsAdmin());
            if (!success) return NotFound();
            return NoContent();
        }

        /// <summary>
        /// Lists all slots for a given lot, subject to the same ownership scoping.
        /// </summary>
        [HttpGet("{id}/slots")]
        [ProducesResponseType(typeof(List<ParkingSlotDto>), 200)]
        public async Task<IActionResult> GetSlots(int id)
        {
            var slots = await _service.GetSlotsForLotAsync(id, GetUserId(), IsAdmin());
            return Ok(slots);
        }

        /// <summary>
        /// Adds a new slot to a lot. Operator/Admin only.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/parkinglots/1/slots
        ///     {
        ///        "slotNumber": "A1",
        ///        "hourlyRate": 50
        ///     }
        /// </remarks>
        /// <response code="201">Slot created, status defaults to Available.</response>
        /// <response code="403">Lot not owned by the calling Operator.</response>
        [HttpPost("{id}/slots")]
        [Authorize(Roles = "Operator,Admin")]
        [ProducesResponseType(typeof(ParkingSlotDto), 201)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> CreateSlot(int id, [FromBody] CreateParkingSlotDto dto)
        {
            var slot = await _service.CreateSlotAsync(id, dto, GetUserId(), IsAdmin());
            if (slot == null) return Forbid();
            return CreatedAtAction(nameof(GetSlots), new { id }, slot);
        }
    }
}