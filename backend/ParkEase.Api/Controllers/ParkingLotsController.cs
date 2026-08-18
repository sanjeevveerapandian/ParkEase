using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkEase.Api.DTOs;
using ParkEase.Api.Services;

namespace ParkEase.Api.Controllers
{
    [ApiController]
    [Route("api/parkinglots")]
    [Authorize(Roles = "Admin,Operator")] // both roles can reach this controller;
                                          // the SERVICE layer enforces who sees what
    public class ParkingLotsController : ControllerBase
    {
        private readonly IParkingLotService _service;

        public ParkingLotsController(IParkingLotService service)
        {
            _service = service;
        }

        // Pulls the user's ID out of the validated JWT — this is the "Sub" claim
        // we set back in AuthService.GenerateAuthResponse.
        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);

        private bool IsAdmin() => User.IsInRole("Admin");

        [HttpGet]
        public async Task<IActionResult> GetLots()
        {
            var lots = await _service.GetLotsAsync(GetUserId(), IsAdmin());
            return Ok(lots);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLot(int id)
        {
            var lot = await _service.GetLotByIdAsync(id, GetUserId(), IsAdmin());
            if (lot == null) return NotFound();
            return Ok(lot);
        }

        [HttpPost]
        [Authorize(Roles = "Operator")] // only Operators create lots (they own what they create)
        public async Task<IActionResult> CreateLot([FromBody] CreateParkingLotDto dto)
        {
            var lot = await _service.CreateLotAsync(dto, GetUserId());
            return CreatedAtAction(nameof(GetLot), new { id = lot.Id }, lot);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLot(int id)
        {
            var success = await _service.DeleteLotAsync(id, GetUserId(), IsAdmin());
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpGet("{id}/slots")]
        public async Task<IActionResult> GetSlots(int id)
        {
            var slots = await _service.GetSlotsForLotAsync(id, GetUserId(), IsAdmin());
            return Ok(slots);
        }

        [HttpPost("{id}/slots")]
        [Authorize(Roles = "Operator,Admin")]
        public async Task<IActionResult> CreateSlot(int id, [FromBody] CreateParkingSlotDto dto)
        {
            var slot = await _service.CreateSlotAsync(id, dto, GetUserId(), IsAdmin());
            if (slot == null) return Forbid();
            return CreatedAtAction(nameof(GetSlots), new { id }, slot);
        }
    }
}