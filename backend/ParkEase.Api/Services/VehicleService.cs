using Microsoft.EntityFrameworkCore;
using ParkEase.Api.Data;
using ParkEase.Api.DTOs;
using ParkEase.Api.Models;

namespace ParkEase.Api.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly ApplicationDbContext _context;

        public VehicleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<List<VehicleDto>> GetMineAsync(int ownerId) =>
            _context.Vehicles
                .Where(v => v.OwnerId == ownerId && v.IsActive)
                .OrderBy(v => v.VehicleNumber)
                .Select(v => new VehicleDto
                {
                    Id = v.Id,
                    VehicleNumber = v.VehicleNumber,
                    Type = v.Type.ToString(),
                    CreatedAt = v.CreatedAt
                })
                .ToListAsync();

        public async Task<ServiceResult<VehicleDto>> AddAsync(CreateVehicleDto dto, int ownerId)
        {
            var plate = VehicleNumber.Normalize(dto.VehicleNumber);
            if (!VehicleNumber.IsValid(plate))
                return ServiceResult<VehicleDto>.Fail(ServiceErrorCode.Validation, "Enter a valid vehicle number, e.g. TN09AB1234.");
            if (!Enum.TryParse<VehicleType>(dto.Type, ignoreCase: true, out var type))
                return ServiceResult<VehicleDto>.Fail(ServiceErrorCode.Validation, "Vehicle type must be Car or Bike.");

            var existing = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.OwnerId == ownerId && v.VehicleNumber == plate);

            if (existing is { IsActive: true })
                return ServiceResult<VehicleDto>.Fail(ServiceErrorCode.Conflict, $"{plate} is already in your vehicles.");

            Vehicle vehicle;
            if (existing != null)
            {
                // Previously removed (archived) — bring it back rather than hit the unique index.
                existing.IsActive = true;
                existing.Type = type;
                vehicle = existing;
            }
            else
            {
                vehicle = new Vehicle { OwnerId = ownerId, VehicleNumber = plate, Type = type };
                _context.Vehicles.Add(vehicle);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
            {
                return ServiceResult<VehicleDto>.Fail(ServiceErrorCode.Conflict, $"{plate} is already in your vehicles.");
            }

            return ServiceResult<VehicleDto>.Ok(new VehicleDto
            {
                Id = vehicle.Id,
                VehicleNumber = vehicle.VehicleNumber,
                Type = vehicle.Type.ToString(),
                CreatedAt = vehicle.CreatedAt
            });
        }

        public async Task<ServiceResult<bool>> RemoveAsync(int vehicleId, int ownerId)
        {
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == vehicleId && v.OwnerId == ownerId && v.IsActive);
            if (vehicle == null)
                return ServiceResult<bool>.Fail(ServiceErrorCode.NotFound, "Vehicle not found.");

            var hasLiveBooking = await _context.VehicleBookings.AnyAsync(b =>
                b.VehicleId == vehicleId
                && (b.Status == BookingStatus.Reserved || b.Status == BookingStatus.Active));
            if (hasLiveBooking)
                return ServiceResult<bool>.Fail(ServiceErrorCode.Conflict,
                    $"{vehicle.VehicleNumber} has a live booking. Exit or cancel it before removing the vehicle.");

            // Billing history points at the vehicle, so keep the row if it has any bookings.
            var hasHistory = await _context.VehicleBookings.AnyAsync(b => b.VehicleId == vehicleId);
            if (hasHistory)
                vehicle.IsActive = false;
            else
                _context.Vehicles.Remove(vehicle);

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }
    }
}
