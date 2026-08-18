using ParkEase.Api.DTOs;

namespace ParkEase.Api.Services
{
    public interface IParkingLotService
    {
        // requestingUserId + isAdmin let the service enforce "operators only see their own lots"
        // without the controller needing to know the business rule itself.
        Task<List<ParkingLotDto>> GetLotsAsync(int requestingUserId, bool isAdmin);
        Task<ParkingLotDto?> GetLotByIdAsync(int lotId, int requestingUserId, bool isAdmin);
        Task<ParkingLotDto> CreateLotAsync(CreateParkingLotDto dto, int operatorId);
        Task<bool> DeleteLotAsync(int lotId, int requestingUserId, bool isAdmin);

        Task<List<ParkingSlotDto>> GetSlotsForLotAsync(int lotId, int requestingUserId, bool isAdmin);
        Task<ParkingSlotDto?> CreateSlotAsync(int lotId, CreateParkingSlotDto dto, int requestingUserId, bool isAdmin);
    }
}