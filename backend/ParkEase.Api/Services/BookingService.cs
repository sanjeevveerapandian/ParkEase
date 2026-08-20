using Microsoft.EntityFrameworkCore;
using ParkEase.Api.Data;
using ParkEase.Api.DTOs;
using ParkEase.Api.Models;

namespace ParkEase.Api.Services
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        // Business rules, named as constants so they're documented and easy to tune —
        // never "magic numbers" buried inline in the calculation.
        private const int OverstayThresholdHours = 24;
        private const decimal OverstayPenaltyMultiplier = 1.5m; // 50% surcharge on overstay hours

        public BookingService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
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

            var driver = await _context.Users.FindAsync(driverId);
            if (driver != null)
            {
                await _notificationService.SendBookingConfirmationAsync(booking, driver.Email);
            }

            return ToDto(booking, slot.SlotNumber, slot.HourlyRate);
        }

        public async Task<BookingDto?> ExitBookingAsync(int bookingId, int requestingUserId, bool canExitAnyBooking)
        {
            var booking = await _context.VehicleBookings
                .Include(b => b.ParkingSlot)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return null;
            if (booking.Status != BookingStatus.Active) return null; // already exited/cancelled

            // A driver can only exit their OWN booking; Admin/Operator can exit any
            // (e.g. an operator manually closing out a booking at the barrier).
            if (!canExitAnyBooking && booking.UserId != requestingUserId) return null;

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

            var driverUser = await _context.Users.FindAsync(booking.UserId);
            if (driverUser != null)
            {
                await _notificationService.SendExitReceiptAsync(booking, driverUser.Email);
            }

            return ToDto(booking, booking.ParkingSlot.SlotNumber, hourlyRate);
        }

        // A single SQL-translated projection shared by both list queries below — EF Core
        // pushes this straight into the SELECT clause (with an implicit join for the
        // ParkingSlot fields), so only the columns actually needed cross the wire, and
        // nothing is tracked. No .Include() needed: that's only for materializing full
        // related entities, not for projecting a few of their columns.
        private static readonly System.Linq.Expressions.Expression<Func<VehicleBooking, BookingDto>> ProjectToDto =
            b => new BookingDto
            {
                Id = b.Id,
                VehicleNumber = b.VehicleNumber,
                ParkingSlotId = b.ParkingSlotId,
                SlotNumber = b.ParkingSlot!.SlotNumber,
                HourlyRate = b.ParkingSlot!.HourlyRate,
                EntryTime = b.EntryTime,
                ExitTime = b.ExitTime,
                TotalFee = b.TotalFee,
                PenaltyFee = b.PenaltyFee,
                Status = b.Status.ToString()
            };

        public async Task<List<BookingDto>> GetMyBookingsAsync(int driverId)
        {
            return await _context.VehicleBookings
                .Where(b => b.UserId == driverId)
                .OrderByDescending(b => b.EntryTime)
                .Select(ProjectToDto)
                .ToListAsync();
        }

        public async Task<List<BookingDto>> GetBookingsForLotAsync(int lotId, int requestingUserId, bool isAdmin)
        {
            var lot = await _context.ParkingLots.FindAsync(lotId);
            if (lot == null) return new List<BookingDto>();
            if (!isAdmin && lot.OperatorId != requestingUserId) return new List<BookingDto>();

            return await _context.VehicleBookings
                .Where(b => b.ParkingSlot!.ParkingLotId == lotId)
                .OrderByDescending(b => b.EntryTime)
                .Select(ProjectToDto)
                .ToListAsync();
        }

        // hourlyRate is passed explicitly rather than read from b.ParkingSlot — that
        // navigation isn't loaded on the CreateBookingAsync path (a freshly-tracked
        // entity with only ParkingSlotId set), so reading it there would NRE.
        private static BookingDto ToDto(VehicleBooking b, string slotNumber, decimal hourlyRate) => new()
        {
            Id = b.Id,
            VehicleNumber = b.VehicleNumber,
            ParkingSlotId = b.ParkingSlotId,
            SlotNumber = slotNumber,
            HourlyRate = hourlyRate,
            EntryTime = b.EntryTime,
            ExitTime = b.ExitTime,
            TotalFee = b.TotalFee,
            PenaltyFee = b.PenaltyFee,
            Status = b.Status.ToString()
        };
    }
}