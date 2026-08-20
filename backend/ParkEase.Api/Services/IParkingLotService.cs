using ParkEase.Api.DTOs;

namespace ParkEase.Api.Services
{
    public interface IParkingLotService
    {
        // requestingUserId + isAdmin let the service enforce "operators only see their own lots"
        // without the controller needing to know the business rule itself.
        Task<List<ParkingLotDto>> GetLotsAsync(int requestingUserId, bool isAdmin);

        // Unfiltered, read-only listing for Drivers to discover lots to park in — deliberately
        // separate from GetLotsAsync, which is ownership-scoped for the Operator/Admin management
        // dashboards. Every lot is fair game to search when you're looking for a place to park.
        Task<List<ParkingLotDto>> GetAllLotsForBrowsingAsync();
        Task<ParkingLotDto?> GetLotByIdAsync(int lotId, int requestingUserId, bool isAdmin);
        Task<ParkingLotDto> CreateLotAsync(CreateParkingLotDto dto, int operatorId);
        Task<bool> DeleteLotAsync(int lotId, int requestingUserId, bool isAdmin);

        Task<List<ParkingSlotDto>> GetSlotsForLotAsync(int lotId);
        Task<ParkingSlotDto?> CreateSlotAsync(int lotId, CreateParkingSlotDto dto, int requestingUserId, bool isAdmin);
    }
}