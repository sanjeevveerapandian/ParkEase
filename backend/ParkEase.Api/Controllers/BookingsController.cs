using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkEase.Api.Authorization;
using ParkEase.Api.DTOs;
using ParkEase.Api.Services;

namespace ParkEase.Api.Controllers
{
    /// <summary>
    /// The booking lifecycle: Reserved → (check-in) Active → (exit) Completed/Overstayed,
    /// with Cancelled and Expired as side exits. See docs/FLOW.md.
    /// </summary>
    [ApiController]
    [Route("api/bookings")]
    [Authorize]
    public class BookingsController : ApiControllerBase
    {
        private readonly IBookingService _service;

        public BookingsController(IBookingService service)
        {
            _service = service;
        }

        /// <summary>
        /// Step 1 — reserves a slot for one of the driver's registered vehicles. Driver-only.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/bookings
        ///     {
        ///        "vehicleId": 3,
        ///        "parkingSlotId": 12
        ///     }
        ///
        /// The slot is held for the reservation window (15 minutes by default); billing does not
        /// start until check-in. The vehicle must be the driver's own and match the slot's type
        /// (Car/Bike), and must not already have a live booking. The driver is identified from the
        /// JWT. Sends an SMS and email confirmation.
        /// </remarks>
        /// <response code="201">Slot reserved.</response>
        /// <response code="400">Vehicle type doesn't match the slot's type.</response>
        /// <response code="404">Vehicle or slot not found.</response>
        /// <response code="409">Slot unavailable, or the vehicle already has a live booking.</response>
        [HttpPost]
        [Authorize(Policy = AuthPolicies.DriverOnly)]
        [ProducesResponseType(typeof(BookingDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Reserve([FromBody] CreateBookingDto dto)
        {
            var result = await _service.ReserveAsync(dto, GetUserId());
            if (!result.Succeeded) return ErrorResult(result);
            return CreatedAtAction(nameof(GetMyBookings), result.Value);
        }

        /// <summary>
        /// Step 2 — checks the vehicle in: the slot becomes Occupied and billing starts.
        /// </summary>
        /// <remarks>
        /// Allowed for the driver who made the booking, the operator who owns the lot, or an admin.
        /// Only Reserved bookings can be checked in, and only before the hold expires.
        /// </remarks>
        [HttpPost("{id}/check-in")]
        [Authorize(Policy = AuthPolicies.AnyAuthenticatedUser)]
        [ProducesResponseType(typeof(BookingDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> CheckIn(int id)
        {
            var result = await _service.CheckInAsync(id, GetActor());
            return result.Succeeded ? Ok(result.Value) : ErrorResult(result);
        }

        /// <summary>
        /// Step 3 — checks the vehicle out: calculates the fee (plus overstay penalty if any),
        /// frees the slot, and sends the exit receipt.
        /// </summary>
        /// <remarks>
        /// Same access rule as check-in. Only Active (checked-in) bookings can be exited.
        /// </remarks>
        [HttpPost("{id}/exit")]
        [Authorize(Policy = AuthPolicies.AnyAuthenticatedUser)]
        [ProducesResponseType(typeof(BookingDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Exit(int id)
        {
            var result = await _service.ExitAsync(id, GetActor());
            return result.Succeeded ? Ok(result.Value) : ErrorResult(result);
        }

        /// <summary>
        /// Cancels a reservation that hasn't been checked in yet. Free of charge.
        /// </summary>
        [HttpPost("{id}/cancel")]
        [Authorize(Policy = AuthPolicies.AnyAuthenticatedUser)]
        [ProducesResponseType(typeof(BookingDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _service.CancelAsync(id, GetActor());
            return result.Succeeded ? Ok(result.Value) : ErrorResult(result);
        }

        /// <summary>
        /// Returns the calling driver's own booking history, most recent first.
        /// </summary>
        [HttpGet("mine")]
        [Authorize(Policy = AuthPolicies.DriverOnly)]
        [ProducesResponseType(typeof(List<BookingDto>), 200)]
        public async Task<IActionResult> GetMyBookings()
        {
            return Ok(await _service.GetMyBookingsAsync(GetUserId()));
        }

        /// <summary>
        /// Returns all bookings for a lot — who is mapped to which slot, and their status.
        /// Operator/Admin only; Operators only see lots they own.
        /// </summary>
        [HttpGet("lot/{lotId}")]
        [Authorize(Policy = AuthPolicies.OperatorOrAdmin)]
        [ProducesResponseType(typeof(List<BookingDto>), 200)]
        public async Task<IActionResult> GetBookingsForLot(int lotId)
        {
            return Ok(await _service.GetBookingsForLotAsync(lotId, GetActor()));
        }
    }
}
