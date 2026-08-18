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

        [HttpPost]
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
        {
            var booking = await _service.CreateBookingAsync(dto, GetUserId());
            if (booking == null) return BadRequest(new { message = "Slot not found or not available." });
            return CreatedAtAction(nameof(GetMyBookings), booking);
        }

        [HttpPost("{id}/exit")]
        public async Task<IActionResult> ExitBooking(int id)
        {
            var booking = await _service.ExitBookingAsync(id, GetUserId(), IsAdmin());
            if (booking == null) return NotFound(new { message = "Booking not found, already closed, or not yours." });
            return Ok(booking);
        }

        [HttpGet("mine")]
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> GetMyBookings()
        {
            var bookings = await _service.GetMyBookingsAsync(GetUserId());
            return Ok(bookings);
        }

        [HttpGet("lot/{lotId}")]
        [Authorize(Roles = "Operator,Admin")]
        public async Task<IActionResult> GetBookingsForLot(int lotId)
        {
            var bookings = await _service.GetBookingsForLotAsync(lotId, GetUserId(), IsAdmin());
            return Ok(bookings);
        }
    }
}