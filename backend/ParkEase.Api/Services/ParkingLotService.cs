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

        public async Task<List<ParkingLotDto>> GetLotsAsync(int requestingUserId, bool isAdmin)
        {
            // Admins see every lot. Operators see ONLY lots where they are the OperatorId.
            // This single line is the caselet's "Operator dashboard restricted to slots/lots
            // under their management" requirement, implemented.
            var query = _context.ParkingLots.AsQueryable();
            if (!isAdmin)
            {
                query = query.Where(l => l.OperatorId == requestingUserId);
            }

            return await query.Select(l => new ParkingLotDto
            {
                Id = l.Id,
                Name = l.Name,
                Address = l.Address,
                OperatorId = l.OperatorId,
                TotalSlots = l.Slots.Count,
                AvailableSlots = l.Slots.Count(s => s.Status == SlotStatus.Available)
            }).ToListAsync();
        }

        public async Task<ParkingLotDto?> GetLotByIdAsync(int lotId, int requestingUserId, bool isAdmin)
        {
            var lot = await _context.ParkingLots.Include(l => l.Slots)
                .FirstOrDefaultAsync(l => l.Id == lotId);

            if (lot == null) return null;

            // Even with the direct ID, an Operator can't read a lot they don't own.
            if (!isAdmin && lot.OperatorId != requestingUserId) return null;

            return new ParkingLotDto
            {
                Id = lot.Id,
                Name = lot.Name,
                Address = lot.Address,
                OperatorId = lot.OperatorId,
                TotalSlots = lot.Slots.Count,
                AvailableSlots = lot.Slots.Count(s => s.Status == SlotStatus.Available)
            };
        }

        public async Task<ParkingLotDto> CreateLotAsync(CreateParkingLotDto dto, int operatorId)
        {
            var lot = new ParkingLot
            {
                Name = dto.Name,
                Address = dto.Address,
                OperatorId = operatorId // taken from JWT claim, not client input — see DTO note above
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

        public async Task<bool> DeleteLotAsync(int lotId, int requestingUserId, bool isAdmin)
        {
            var lot = await _context.ParkingLots.FindAsync(lotId);
            if (lot == null) return false;
            if (!isAdmin && lot.OperatorId != requestingUserId) return false;

            _context.ParkingLots.Remove(lot);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ParkingSlotDto>> GetSlotsForLotAsync(int lotId, int requestingUserId, bool isAdmin)
        {
            var lot = await _context.ParkingLots.Include(l => l.Slots)
                .FirstOrDefaultAsync(l => l.Id == lotId);

            if (lot == null) return new List<ParkingSlotDto>();
            if (!isAdmin && lot.OperatorId != requestingUserId) return new List<ParkingSlotDto>();

            return lot.Slots.Select(s => new ParkingSlotDto
            {
                Id = s.Id,
                SlotNumber = s.SlotNumber,
                Status = s.Status.ToString(),
                HourlyRate = s.HourlyRate,
                ParkingLotId = s.ParkingLotId
            }).ToList();
        }

        public async Task<ParkingSlotDto?> CreateSlotAsync(int lotId, CreateParkingSlotDto dto, int requestingUserId, bool isAdmin)
        {
            var lot = await _context.ParkingLots.FindAsync(lotId);
            if (lot == null) return null;
            if (!isAdmin && lot.OperatorId != requestingUserId) return null;

            var slot = new ParkingSlot
            {
                SlotNumber = dto.SlotNumber,
                HourlyRate = dto.HourlyRate,
                ParkingLotId = lotId,
                Status = SlotStatus.Available
            };

            _context.ParkingSlots.Add(slot);
            await _context.SaveChangesAsync();

            return new ParkingSlotDto
            {
                Id = slot.Id,
                SlotNumber = slot.SlotNumber,
                Status = slot.Status.ToString(),
                HourlyRate = slot.HourlyRate,
                ParkingLotId = slot.ParkingLotId
            };
        }
    }
}