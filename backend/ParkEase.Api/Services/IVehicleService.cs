using ParkEase.Api.DTOs;

namespace ParkEase.Api.Services
{
    public interface IVehicleService
    {
        Task<List<VehicleDto>> GetMineAsync(int ownerId);
        Task<ServiceResult<VehicleDto>> AddAsync(CreateVehicleDto dto, int ownerId);
        Task<ServiceResult<bool>> RemoveAsync(int vehicleId, int ownerId);
    }
}
