using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkEase.Api.DTOs;
using ParkEase.Api.Services;

namespace ParkEase.Api.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _service;

        public BookingsController(IBookingService service)
        {
            _service = service;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);

        private bool IsAdmin() => User.IsInRole("Admin");

        /// <summary>
        /// Books a vehicle into an available parking slot. Driver-only.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/bookings
        ///     {
        ///        "parkingSlotId": 1,
        ///        "vehicleNumber": "TN01AB1234"
        ///     }
        ///
        /// The driver is identified from the JWT token, not from the request body.
        /// Triggers a booking-confirmation email to the driver on success.
        /// </remarks>
        /// <response code="201">Booking created — slot marked Occupied.</response>
        /// <response code="400">Slot not found, or not currently available.</response>
        [HttpPost]
        [Authorize(Roles = "Driver")]
        [ProducesResponseType(typeof(BookingDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
        {
            var booking = await _service.CreateBookingAsync(dto, GetUserId());
            if (booking == null) return BadRequest(new { message = "Slot not found or not available." });
            return CreatedAtAction(nameof(GetMyBookings), booking);
        }

        /// <summary>
        /// Closes out an active booking: calculates the fee (and overstay penalty if applicable),
        /// frees the slot, and sends an exit-receipt email.
        /// </summary>
        /// <response code="200">Booking closed — returns the final fee breakdown.</response>
        /// <response code="404">Booking not found, already closed, or not owned by the caller.</response>
        [HttpPost("{id}/exit")]
        [ProducesResponseType(typeof(BookingDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ExitBooking(int id)
        {
            var booking = await _service.ExitBookingAsync(id, GetUserId(), IsAdmin());
            if (booking == null) return NotFound(new { message = "Booking not found, already closed, or not yours." });
            return Ok(booking);
        }

        /// <summary>
        /// Returns the calling driver's own booking history, most recent first.
        /// </summary>
        [HttpGet("mine")]
        [Authorize(Roles = "Driver")]
        [ProducesResponseType(typeof(List<BookingDto>), 200)]
        public async Task<IActionResult> GetMyBookings()
        {
            var bookings = await _service.GetMyBookingsAsync(GetUserId());
            return Ok(bookings);
        }

        /// <summary>
        /// Returns all bookings for a given lot. Operator/Admin only — Operators
        /// only see bookings for lots they own.
        /// </summary>
        [HttpGet("lot/{lotId}")]
        [Authorize(Roles = "Operator,Admin")]
        [ProducesResponseType(typeof(List<BookingDto>), 200)]
        public async Task<IActionResult> GetBookingsForLot(int lotId)
        {
            var bookings = await _service.GetBookingsForLotAsync(lotId, GetUserId(), IsAdmin());
            return Ok(bookings);
        }
    }
}