using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkEase.Api.Authorization;
using ParkEase.Api.DTOs;
using ParkEase.Api.Services;

namespace ParkEase.Api.Controllers
{
    [ApiController]
    [Route("api/parkinglots")]
    [Authorize]
    public class ParkingLotsController : ApiControllerBase
    {
        private readonly IParkingLotService _service;

        public ParkingLotsController(IParkingLotService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lists parking lots. Admins see every lot; Operators see only lots they own.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = AuthPolicies.OperatorOrAdmin)]
        [ProducesResponseType(typeof(List<ParkingLotDto>), 200)]
        public async Task<IActionResult> GetLots()
        {
            return Ok(await _service.GetLotsAsync(GetActor()));
        }

        /// <summary>
        /// Lists every parking lot, unfiltered — the driver-facing "browse lots to park in" read.
        /// Open to any authenticated role. Not ownership-scoped like <see cref="GetLots"/>, which
        /// backs the Operator/Admin management dashboards instead.
        /// </summary>
        [HttpGet("browse")]
        [Authorize(Policy = AuthPolicies.AnyAuthenticatedUser)]
        [ProducesResponseType(typeof(List<ParkingLotDto>), 200)]
        public async Task<IActionResult> BrowseLots()
        {
            return Ok(await _service.GetAllLotsForBrowsingAsync());
        }

        /// <summary>
        /// Gets a single lot by id, subject to the same ownership scoping as the list endpoint.
        /// </summary>
        /// <response code="200">Lot found and accessible to the caller.</response>
        /// <response code="404">Lot not found, or not owned by the caller.</response>
        [HttpGet("{id}")]
        [Authorize(Policy = AuthPolicies.OperatorOrAdmin)]
        [ProducesResponseType(typeof(ParkingLotDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetLot(int id)
        {
            var lot = await _service.GetLotByIdAsync(id, GetActor());
            if (lot == null) return NotFound(new { message = "Parking lot not found." });
            return Ok(lot);
        }

        /// <summary>
        /// Creates a new parking lot. Operator or Admin.
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
        /// The owning user (Operator or Admin) is taken from the JWT token, not the request body.
        /// An Admin who creates a lot becomes its OperatorId, same as an Operator would — Admins
        /// already see every lot regardless of ownership, so this doesn't change their visibility.
        /// </remarks>
        /// <response code="201">Lot created.</response>
        [HttpPost]
        [Authorize(Policy = AuthPolicies.OperatorOrAdmin)]
        [ProducesResponseType(typeof(ParkingLotDto), 201)]
        public async Task<IActionResult> CreateLot([FromBody] CreateParkingLotDto dto)
        {
            var lot = await _service.CreateLotAsync(dto, GetUserId());
            return CreatedAtAction(nameof(GetLot), new { id = lot.Id }, lot);
        }

        /// <summary>
        /// Deletes a lot. Admin can delete any lot; Operators can only delete their own.
        /// A lot can only be deleted once it has no slots.
        /// </summary>
        /// <response code="204">Lot deleted.</response>
        /// <response code="404">Lot not found, or not owned by the caller.</response>
        /// <response code="409">The lot still has slots.</response>
        [HttpDelete("{id}")]
        [Authorize(Policy = AuthPolicies.OperatorOrAdmin)]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> DeleteLot(int id)
        {
            var result = await _service.DeleteLotAsync(id, GetActor());
            return result.Succeeded ? NoContent() : ErrorResult(result);
        }

        /// <summary>
        /// Lists all slots for a given lot, with their live status (Available / Reserved / Occupied /
        /// Maintenance) and vehicle type. Open to any authenticated role (including Driver), since
        /// browsing availability is required for the booking flow — not ownership-scoped like the
        /// management endpoints above.
        /// </summary>
        [HttpGet("{id}/slots")]
        [Authorize(Policy = AuthPolicies.AnyAuthenticatedUser)]
        [ProducesResponseType(typeof(List<ParkingSlotDto>), 200)]
        public async Task<IActionResult> GetSlots(int id)
        {
            return Ok(await _service.GetSlotsForLotAsync(id));
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
        ///        "hourlyRate": 50,
        ///        "vehicleType": "Car"
        ///     }
        ///
        /// vehicleType is Car (default) or Bike; only vehicles of that type can be booked into the slot.
        /// </remarks>
        /// <response code="201">Slot created, status defaults to Available.</response>
        /// <response code="403">Lot not owned by the calling Operator.</response>
        /// <response code="409">A slot with that number already exists in the lot.</response>
        [HttpPost("{id}/slots")]
        [Authorize(Policy = AuthPolicies.OperatorOrAdmin)]
        [ProducesResponseType(typeof(ParkingSlotDto), 201)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> CreateSlot(int id, [FromBody] CreateParkingSlotDto dto)
        {
            var result = await _service.CreateSlotAsync(id, dto, GetActor());
            if (!result.Succeeded) return ErrorResult(result);
            return CreatedAtAction(nameof(GetSlots), new { id }, result.Value);
        }

        /// <summary>
        /// Takes a free slot out of service (Maintenance) or returns it to Available.
        /// Reserved and Occupied slots can't be changed — they're driven by bookings.
        /// </summary>
        [HttpPatch("{id}/slots/{slotId}/status")]
        [Authorize(Policy = AuthPolicies.OperatorOrAdmin)]
        [ProducesResponseType(typeof(ParkingSlotDto), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> UpdateSlotStatus(int id, int slotId, [FromBody] UpdateSlotStatusDto dto)
        {
            var result = await _service.UpdateSlotStatusAsync(id, slotId, dto.Status, GetActor());
            return result.Succeeded ? Ok(result.Value) : ErrorResult(result);
        }

        /// <summary>
        /// Deletes a slot that is free and has never been booked. Slots with booking history can't
        /// be deleted (set them to Maintenance instead) so billing records stay intact.
        /// </summary>
        [HttpDelete("{id}/slots/{slotId}")]
        [Authorize(Policy = AuthPolicies.OperatorOrAdmin)]
        [ProducesResponseType(204)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> DeleteSlot(int id, int slotId)
        {
            var result = await _service.DeleteSlotAsync(id, slotId, GetActor());
            return result.Succeeded ? NoContent() : ErrorResult(result);
        }
    }
}
