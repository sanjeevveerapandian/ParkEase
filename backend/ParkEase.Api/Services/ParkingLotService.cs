using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ParkEase.Api.Data;
using ParkEase.Api.DTOs;
using ParkEase.Api.Models;

namespace ParkEase.Api.Services
{
    public class ParkingLotService : IParkingLotService
    {
        private readonly ApplicationDbContext _context;

        public ParkingLotService(ApplicationDbContext context)
        {
            _context = context;
        }

        private static readonly Expression<Func<ParkingLot, ParkingLotDto>> ProjectLot =
            l => new ParkingLotDto
            {
                Id = l.Id,
                Name = l.Name,
                Address = l.Address,
                OperatorId = l.OperatorId,
                TotalSlots = l.Slots.Count,
                AvailableSlots = l.Slots.Count(s => s.Status == SlotStatus.Available)
            };

        private static readonly Expression<Func<ParkingSlot, ParkingSlotDto>> ProjectSlot =
            s => new ParkingSlotDto
            {
                Id = s.Id,
                SlotNumber = s.SlotNumber,
                Status = s.Status.ToString(),
                VehicleType = s.VehicleType.ToString(),
                HourlyRate = s.HourlyRate,
                ParkingLotId = s.ParkingLotId
            };

        // Operators manage only lots they own; admins manage any.
        private static bool CanManage(ParkingLot lot, Actor actor) =>
            actor.IsAdmin || lot.OperatorId == actor.UserId;

        // ------------------------------------------------------------------ lots ----

        public async Task<List<ParkingLotDto>> GetLotsAsync(Actor actor)
        {
            var query = _context.ParkingLots.AsQueryable();
            if (!actor.IsAdmin)
                query = query.Where(l => l.OperatorId == actor.UserId);

            return await query.Select(ProjectLot).ToListAsync();
        }

        public Task<List<ParkingLotDto>> GetAllLotsForBrowsingAsync() =>
            _context.ParkingLots.Select(ProjectLot).ToListAsync();

        public async Task<ParkingLotDto?> GetLotByIdAsync(int lotId, Actor actor)
        {
            var lot = await _context.ParkingLots.Where(l => l.Id == lotId).Select(ProjectLot).FirstOrDefaultAsync();
            if (lot == null) return null;

            // Even with the direct id, an Operator can't read a lot they don't own.
            return actor.IsAdmin || lot.OperatorId == actor.UserId ? lot : null;
        }

        public async Task<ParkingLotDto> CreateLotAsync(CreateParkingLotDto dto, int ownerId)
        {
            var lot = new ParkingLot
            {
                Name = dto.Name.Trim(),
                Address = dto.Address.Trim(),
                OperatorId = ownerId // from the JWT, never from client input
            };

            _context.ParkingLots.Add(lot);
            await _context.SaveChangesAsync();

            return new ParkingLotDto
            {
                Id = lot.Id,
                Name = lot.Name,
                Address = lot.Address,
                OperatorId = lot.OperatorId,
                TotalSlots = 0,
                AvailableSlots = 0
            };
        }

        public async Task<ServiceResult<bool>> DeleteLotAsync(int lotId, Actor actor)
        {
            var lot = await _context.ParkingLots.FindAsync(lotId);
            if (lot == null || !CanManage(lot, actor))
                return ServiceResult<bool>.Fail(ServiceErrorCode.NotFound, "Parking lot not found.");

            var slotCount = await _context.ParkingSlots.CountAsync(s => s.ParkingLotId == lotId);
            if (slotCount > 0)
                return ServiceResult<bool>.Fail(ServiceErrorCode.Conflict,
                    $"This lot still has {slotCount} slot(s). Delete its slots first. Lots whose slots have booking history can't be deleted, to keep billing records intact.");

            _context.ParkingLots.Remove(lot);
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        // ----------------------------------------------------------------- slots ----

        // No ownership check on reads: drivers need to see a lot's slots to book one.
        public Task<List<ParkingSlotDto>> GetSlotsForLotAsync(int lotId) =>
            _context.ParkingSlots
                .Where(s => s.ParkingLotId == lotId)
                .OrderBy(s => s.SlotNumber)
                .Select(ProjectSlot)
                .ToListAsync();

        public async Task<ServiceResult<ParkingSlotDto>> CreateSlotAsync(int lotId, CreateParkingSlotDto dto, Actor actor)
        {
            var lot = await _context.ParkingLots.FindAsync(lotId);
            if (lot == null)
                return ServiceResult<ParkingSlotDto>.Fail(ServiceErrorCode.NotFound, "Parking lot not found.");
            if (!CanManage(lot, actor))
                return ServiceResult<ParkingSlotDto>.Fail(ServiceErrorCode.Forbidden, "You don't manage this lot.");

            var number = dto.SlotNumber.Trim().ToUpperInvariant();
            if (await _context.ParkingSlots.AnyAsync(s => s.ParkingLotId == lotId && s.SlotNumber == number))
                return ServiceResult<ParkingSlotDto>.Fail(ServiceErrorCode.Conflict, $"Slot {number} already exists in this lot.");

            var slot = new ParkingSlot
            {
                SlotNumber = number,
                HourlyRate = dto.HourlyRate,
                VehicleType = Enum.Parse<VehicleType>(dto.VehicleType, ignoreCase: true),
                ParkingLotId = lotId,
                Status = SlotStatus.Available
            };

            _context.ParkingSlots.Add(slot);
            await _context.SaveChangesAsync();

            return ServiceResult<ParkingSlotDto>.Ok(
                await _context.ParkingSlots.Where(s => s.Id == slot.Id).Select(ProjectSlot).FirstAsync());
        }

        public async Task<ServiceResult<ParkingSlotDto>> UpdateSlotStatusAsync(int lotId, int slotId, string status, Actor actor)
        {
            var (slot, error) = await LoadManagedSlotAsync<ParkingSlotDto>(lotId, slotId, actor);
            if (slot == null) return error!;

            var target = Enum.Parse<SlotStatus>(status, ignoreCase: true);
            var allowed = (slot.Status, target) is (SlotStatus.Available, SlotStatus.Maintenance)
                                                or (SlotStatus.Maintenance, SlotStatus.Available);
            if (!allowed)
            {
                var message = slot.Status == target
                    ? $"Slot {slot.SlotNumber} is already {slot.Status}."
                    : $"Slot {slot.SlotNumber} is {slot.Status} right now, so it can't be set to {target}. Only free slots can go to maintenance.";
                return ServiceResult<ParkingSlotDto>.Fail(ServiceErrorCode.Conflict, message);
            }

            slot.Status = target;
            await _context.SaveChangesAsync();

            return ServiceResult<ParkingSlotDto>.Ok(
                await _context.ParkingSlots.Where(s => s.Id == slotId).Select(ProjectSlot).FirstAsync());
        }

        public async Task<ServiceResult<bool>> DeleteSlotAsync(int lotId, int slotId, Actor actor)
        {
            var (slot, error) = await LoadManagedSlotAsync<bool>(lotId, slotId, actor);
            if (slot == null) return error!;

            if (slot.Status is SlotStatus.Reserved or SlotStatus.Occupied)
                return ServiceResult<bool>.Fail(ServiceErrorCode.Conflict,
                    $"Slot {slot.SlotNumber} is {slot.Status} right now and can't be deleted.");

            if (await _context.VehicleBookings.AnyAsync(b => b.ParkingSlotId == slotId))
                return ServiceResult<bool>.Fail(ServiceErrorCode.Conflict,
                    $"Slot {slot.SlotNumber} has booking history and can't be deleted. Set it to maintenance instead.");

            _context.ParkingSlots.Remove(slot);
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        private async Task<(ParkingSlot? Slot, ServiceResult<T>? Error)> LoadManagedSlotAsync<T>(int lotId, int slotId, Actor actor)
        {
            var slot = await _context.ParkingSlots.Include(s => s.ParkingLot)
                .FirstOrDefaultAsync(s => s.Id == slotId && s.ParkingLotId == lotId);
            if (slot == null)
                return (null, ServiceResult<T>.Fail(ServiceErrorCode.NotFound, "Parking slot not found."));
            if (!CanManage(slot.ParkingLot!, actor))
                return (null, ServiceResult<T>.Fail(ServiceErrorCode.Forbidden, "You don't manage this lot."));
            return (slot, null);
        }
    }
}
