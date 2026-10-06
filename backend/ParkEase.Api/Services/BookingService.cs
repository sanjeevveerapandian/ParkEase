using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ParkEase.Api.Data;
using ParkEase.Api.DTOs;
using ParkEase.Api.Models;
using ParkEase.Api.Services.Notifications;

namespace ParkEase.Api.Services
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notifications;
        private readonly BookingOptions _options;
        private readonly TimeProvider _clock;

        public BookingService(
            ApplicationDbContext context,
            INotificationService notifications,
            IOptions<BookingOptions> options,
            TimeProvider clock)
        {
            _context = context;
            _notifications = notifications;
            _options = options.Value;
            _clock = clock;
        }

        private DateTime Now => _clock.GetUtcNow().UtcDateTime;

        // ---------------------------------------------------------------- reserve ----

        public async Task<ServiceResult<BookingDto>> ReserveAsync(CreateBookingDto dto, int driverId)
        {
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == dto.VehicleId && v.OwnerId == driverId && v.IsActive);
            if (vehicle == null)
                return Fail(ServiceErrorCode.NotFound, "Vehicle not found. Add it under My Vehicles first.");

            var slot = await _context.ParkingSlots.FirstOrDefaultAsync(s => s.Id == dto.ParkingSlotId);
            if (slot == null)
                return Fail(ServiceErrorCode.NotFound, "Parking slot not found.");

            if (slot.Status != SlotStatus.Available)
            {
                var reason = slot.Status switch
                {
                    SlotStatus.Reserved => $"Slot {slot.SlotNumber} is already reserved by another driver.",
                    SlotStatus.Occupied => $"Slot {slot.SlotNumber} is currently occupied.",
                    _ => $"Slot {slot.SlotNumber} is out of service for maintenance."
                };
                return Fail(ServiceErrorCode.Conflict, reason);
            }

            // The vehicle-to-slot mapping rule: a vehicle only fits a bay built for its type.
            if (vehicle.Type != slot.VehicleType)
                return Fail(ServiceErrorCode.Validation,
                    $"Slot {slot.SlotNumber} is a {slot.VehicleType} slot, but {vehicle.VehicleNumber} is registered as a {vehicle.Type}.");

            // One live booking per vehicle, matched by plate as well as id so the same plate
            // registered under two accounts still can't hold two slots.
            var live = await _context.VehicleBookings
                .Where(b => (b.Status == BookingStatus.Reserved || b.Status == BookingStatus.Active)
                            && (b.VehicleId == vehicle.Id || b.VehicleNumber == vehicle.VehicleNumber))
                .Select(b => new { b.ParkingSlot!.SlotNumber, LotName = b.ParkingSlot.ParkingLot!.Name })
                .FirstOrDefaultAsync();
            if (live != null)
                return Fail(ServiceErrorCode.Conflict,
                    $"{vehicle.VehicleNumber} already has a live booking (slot {live.SlotNumber} at {live.LotName}). Exit or cancel it first.");

            var now = Now;
            var booking = new VehicleBooking
            {
                UserId = driverId,
                VehicleId = vehicle.Id,
                VehicleNumber = vehicle.VehicleNumber,
                ParkingSlotId = slot.Id,
                Status = BookingStatus.Reserved,
                ReservedAt = now,
                ReservationExpiresAt = now.AddMinutes(_options.ReservationHoldMinutes)
            };
            slot.Status = SlotStatus.Reserved;
            _context.VehicleBookings.Add(booking);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Two drivers raced for the same slot; the filtered unique indexes let only one through.
                return Fail(ServiceErrorCode.Conflict, "Someone just took that slot. Please pick another one.");
            }

            await _notifications.NotifyAsync(NotificationType.BookingConfirmation, booking.Id);
            return await ResultAsync(booking.Id);
        }

        // --------------------------------------------------------------- check-in ----

        public async Task<ServiceResult<BookingDto>> CheckInAsync(int bookingId, Actor actor)
        {
            var booking = await LoadManageableAsync(bookingId, actor);
            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.Reserved)
                return Fail(ServiceErrorCode.Conflict, $"Only reserved bookings can be checked in; this one is {booking.Status}.");

            var now = Now;
            if (booking.ReservationExpiresAt <= now)
            {
                await ExpireAsync(booking, now);
                return Fail(ServiceErrorCode.Conflict, "This reservation expired before check-in and the slot was released. Please book again.");
            }

            booking.Status = BookingStatus.Active;
            booking.EntryTime = now;
            booking.ReservationExpiresAt = null;
            booking.ParkingSlot!.Status = SlotStatus.Occupied;
            await _context.SaveChangesAsync();

            await _notifications.NotifyAsync(NotificationType.CheckIn, booking.Id);
            return await ResultAsync(booking.Id);
        }

        // ------------------------------------------------------------------- exit ----

        public async Task<ServiceResult<BookingDto>> ExitAsync(int bookingId, Actor actor)
        {
            var booking = await LoadManageableAsync(bookingId, actor);
            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.Active)
            {
                var reason = booking.Status == BookingStatus.Reserved
                    ? "This booking hasn't been checked in yet. Check in first, or cancel the reservation."
                    : $"This booking is already {booking.Status}.";
                return Fail(ServiceErrorCode.Conflict, reason);
            }

            var exit = Now;
            var fee = FeeCalculator.Calculate(booking.EntryTime!.Value, exit, booking.ParkingSlot!.HourlyRate);

            booking.ExitTime = exit;
            booking.TotalFee = fee.TotalFee;
            booking.PenaltyFee = fee.PenaltyFee;
            booking.Status = fee.Overstayed ? BookingStatus.Overstayed : BookingStatus.Completed;
            booking.ParkingSlot.Status = SlotStatus.Available;
            await _context.SaveChangesAsync();

            await _notifications.NotifyAsync(NotificationType.ExitReceipt, booking.Id);
            return await ResultAsync(booking.Id);
        }

        // ----------------------------------------------------------------- cancel ----

        public async Task<ServiceResult<BookingDto>> CancelAsync(int bookingId, Actor actor)
        {
            var booking = await LoadManageableAsync(bookingId, actor);
            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.Reserved)
            {
                var reason = booking.Status == BookingStatus.Active
                    ? "A parked vehicle can't be cancelled. Exit to close the booking and pay the fee."
                    : $"This booking is already {booking.Status}.";
                return Fail(ServiceErrorCode.Conflict, reason);
            }

            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = Now;
            booking.ReservationExpiresAt = null;
            booking.ParkingSlot!.Status = SlotStatus.Available;
            await _context.SaveChangesAsync();

            await _notifications.NotifyAsync(NotificationType.ReservationCancelled, booking.Id);
            return await ResultAsync(booking.Id);
        }

        // ---------------------------------------------------------------- queries ----

        // Projected straight into SQL: only the needed columns cross the wire and nothing is tracked.
        private static readonly Expression<Func<VehicleBooking, BookingDto>> ProjectToDto = b => new BookingDto
        {
            Id = b.Id,
            Status = b.Status.ToString(),
            VehicleId = b.VehicleId,
            VehicleNumber = b.VehicleNumber,
            VehicleType = b.Vehicle!.Type.ToString(),
            DriverName = b.User!.FullName,
            ParkingSlotId = b.ParkingSlotId,
            SlotNumber = b.ParkingSlot!.SlotNumber,
            ParkingLotId = b.ParkingSlot.ParkingLotId,
            LotName = b.ParkingSlot.ParkingLot!.Name,
            HourlyRate = b.ParkingSlot.HourlyRate,
            ReservedAt = b.ReservedAt,
            ReservationExpiresAt = b.ReservationExpiresAt,
            EntryTime = b.EntryTime,
            ExitTime = b.ExitTime,
            CancelledAt = b.CancelledAt,
            TotalFee = b.TotalFee,
            PenaltyFee = b.PenaltyFee
        };

        public Task<List<BookingDto>> GetMyBookingsAsync(int driverId) =>
            _context.VehicleBookings
                .Where(b => b.UserId == driverId)
                .OrderByDescending(b => b.ReservedAt)
                .Select(ProjectToDto)
                .ToListAsync();

        public async Task<List<BookingDto>> GetBookingsForLotAsync(int lotId, Actor actor)
        {
            var lot = await _context.ParkingLots.FindAsync(lotId);
            if (lot == null || (!actor.IsAdmin && lot.OperatorId != actor.UserId))
                return new List<BookingDto>();

            return await _context.VehicleBookings
                .Where(b => b.ParkingSlot!.ParkingLotId == lotId)
                .OrderByDescending(b => b.ReservedAt)
                .Select(ProjectToDto)
                .ToListAsync();
        }

        // ------------------------------------------------------ background upkeep ----

        public async Task<int> ExpireStaleReservationsAsync()
        {
            var now = Now;
            var ids = await _context.VehicleBookings
                .Where(b => b.Status == BookingStatus.Reserved && b.ReservationExpiresAt <= now)
                .Select(b => b.Id)
                .ToListAsync();

            foreach (var id in ids)
            {
                var booking = await _context.VehicleBookings
                    .Include(b => b.ParkingSlot)
                    .FirstOrDefaultAsync(b => b.Id == id);
                if (booking is { Status: BookingStatus.Reserved })
                    await ExpireAsync(booking, now);
            }
            return ids.Count;
        }

        public async Task<int> SendOverstayWarningsAsync()
        {
            // Warn one hour before the 24h limit (and still warn if we missed that window).
            var cutoff = Now.AddHours(-(FeeCalculator.OverstayThresholdHours - 1));
            var ids = await _context.VehicleBookings
                .Where(b => b.Status == BookingStatus.Active
                            && b.EntryTime <= cutoff
                            && !_context.Notifications.Any(n => n.VehicleBookingId == b.Id
                                                                && n.Type == NotificationType.OverstayWarning))
                .Select(b => b.Id)
                .ToListAsync();

            foreach (var id in ids)
                await _notifications.NotifyAsync(NotificationType.OverstayWarning, id);
            return ids.Count;
        }

        // ---------------------------------------------------------------- helpers ----

        private async Task ExpireAsync(VehicleBooking booking, DateTime now)
        {
            booking.Status = BookingStatus.Expired;
            booking.CancelledAt = now;
            booking.ReservationExpiresAt = null;
            if (booking.ParkingSlot is { Status: SlotStatus.Reserved } slot)
                slot.Status = SlotStatus.Available;
            await _context.SaveChangesAsync();

            await _notifications.NotifyAsync(NotificationType.ReservationExpired, booking.Id);
        }

        // Loads a booking only if this actor may act on it: the driver who made it, the operator
        // who owns the lot it's in, or an admin. Anyone else gets "not found" so booking ids
        // can't be probed.
        private async Task<VehicleBooking?> LoadManageableAsync(int bookingId, Actor actor)
        {
            var booking = await _context.VehicleBookings
                .Include(b => b.ParkingSlot!).ThenInclude(s => s.ParkingLot)
                .FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking == null) return null;

            var allowed = actor.Role switch
            {
                Models.UserRole.Admin => true,
                Models.UserRole.Operator => booking.ParkingSlot!.ParkingLot!.OperatorId == actor.UserId,
                _ => booking.UserId == actor.UserId
            };
            return allowed ? booking : null;
        }

        private async Task<ServiceResult<BookingDto>> ResultAsync(int bookingId) =>
            ServiceResult<BookingDto>.Ok(
                await _context.VehicleBookings.Where(b => b.Id == bookingId).Select(ProjectToDto).FirstAsync());

        private static ServiceResult<BookingDto> NotFound() =>
            Fail(ServiceErrorCode.NotFound, "Booking not found.");

        private static ServiceResult<BookingDto> Fail(ServiceErrorCode code, string message) =>
            ServiceResult<BookingDto>.Fail(code, message);

        // SQL Server error 2601/2627 = unique index / constraint violation.
        private static bool IsUniqueViolation(DbUpdateException ex) =>
            ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 };
    }
}
