using Microsoft.EntityFrameworkCore;
using ParkEase.Api.Data;
using ParkEase.Api.DTOs;
using ParkEase.Api.Models;

namespace ParkEase.Api.Services
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;

        // Business rules, named as constants so they're documented and easy to tune —
        // never "magic numbers" buried inline in the calculation.
        private const int OverstayThresholdHours = 24;
        private const decimal OverstayPenaltyMultiplier = 1.5m; // 50% surcharge on overstay hours

        public BookingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BookingDto?> CreateBookingAsync(CreateBookingDto dto, int driverId)
        {
            var slot = await _context.ParkingSlots.FindAsync(dto.ParkingSlotId);
            if (slot == null) return null;
            if (slot.Status != SlotStatus.Available) return null; // can't book an occupied/maintenance slot

            var booking = new VehicleBooking
            {
                VehicleNumber = dto.VehicleNumber,
                UserId = driverId,
                ParkingSlotId = dto.ParkingSlotId,
                EntryTime = DateTime.UtcNow,
                Status = BookingStatus.Active
            };

            slot.Status = SlotStatus.Occupied; // mark the slot taken immediately
            _context.VehicleBookings.Add(booking);
            await _context.SaveChangesAsync();

            return ToDto(booking, slot.SlotNumber);
        }

        public async Task<BookingDto?> ExitBookingAsync(int bookingId, int requestingUserId, bool isAdmin)
        {
            var booking = await _context.VehicleBookings
                .Include(b => b.ParkingSlot)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return null;
            if (booking.Status != BookingStatus.Active) return null; // already exited/cancelled

            // A driver can only exit their OWN booking; Admin/Operator can exit any
            // (e.g. an operator manually closing out a booking at the barrier).
            if (!isAdmin && booking.UserId != requestingUserId) return null;

            booking.ExitTime = DateTime.UtcNow;
            var duration = booking.ExitTime.Value - booking.EntryTime;

            // Billable hours: always round UP to the next full hour, minimum 1.
            // Standard parking billing convention — 61 minutes bills as 2 hours, not 1.02.
            var billableHours = (int)Math.Ceiling(duration.TotalHours);
            if (billableHours < 1) billableHours = 1;

            var hourlyRate = booking.ParkingSlot!.HourlyRate;

            if (billableHours > OverstayThresholdHours)
            {
                // Normal hours billed at standard rate, hours beyond the threshold
                // billed at the penalty multiplier — not the whole stay penalized.
                var normalHours = OverstayThresholdHours;
                var overstayHours = billableHours - OverstayThresholdHours;

                booking.TotalFee = normalHours * hourlyRate;
                booking.PenaltyFee = overstayHours * hourlyRate * OverstayPenaltyMultiplier;
                booking.Status = BookingStatus.Overstayed;
            }
            else
            {
                booking.TotalFee = billableHours * hourlyRate;
                booking.PenaltyFee = 0;
                booking.Status = BookingStatus.Completed;
            }

            booking.ParkingSlot.Status = SlotStatus.Available; // free up the slot

            await _context.SaveChangesAsync();
            return ToDto(booking, booking.ParkingSlot.SlotNumber);
        }

        public async Task<List<BookingDto>> GetMyBookingsAsync(int driverId)
        {
            return await _context.VehicleBookings
                .Include(b => b.ParkingSlot)
                .Where(b => b.UserId == driverId)
                .OrderByDescending(b => b.EntryTime)
                .Select(b => new BookingDto
                {
                    Id = b.Id,
                    VehicleNumber = b.VehicleNumber,
                    ParkingSlotId = b.ParkingSlotId,
                    SlotNumber = b.ParkingSlot!.SlotNumber,
                    EntryTime = b.EntryTime,
                    ExitTime = b.ExitTime,
                    TotalFee = b.TotalFee,
                    PenaltyFee = b.PenaltyFee,
                    Status = b.Status.ToString()
                }).ToListAsync();
        }

        public async Task<List<BookingDto>> GetBookingsForLotAsync(int lotId, int requestingUserId, bool isAdmin)
        {
            var lot = await _context.ParkingLots.FindAsync(lotId);
            if (lot == null) return new List<BookingDto>();
            if (!isAdmin && lot.OperatorId != requestingUserId) return new List<BookingDto>();

            return await _context.VehicleBookings
                .Include(b => b.ParkingSlot)
                .Where(b => b.ParkingSlot!.ParkingLotId == lotId)
                .OrderByDescending(b => b.EntryTime)
                .Select(b => new BookingDto
                {
                    Id = b.Id,
                    VehicleNumber = b.VehicleNumber,
                    ParkingSlotId = b.ParkingSlotId,
                    SlotNumber = b.ParkingSlot!.SlotNumber,
                    EntryTime = b.EntryTime,
                    ExitTime = b.ExitTime,
                    TotalFee = b.TotalFee,
                    PenaltyFee = b.PenaltyFee,
                    Status = b.Status.ToString()
                }).ToListAsync();
        }

        private static BookingDto ToDto(VehicleBooking b, string slotNumber) => new()
        {
            Id = b.Id,
            VehicleNumber = b.VehicleNumber,
            ParkingSlotId = b.ParkingSlotId,
            SlotNumber = slotNumber,
            EntryTime = b.EntryTime,
            ExitTime = b.ExitTime,
            TotalFee = b.TotalFee,
            PenaltyFee = b.PenaltyFee,
            Status = b.Status.ToString()
        };
    }
}