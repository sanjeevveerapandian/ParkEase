using ParkEase.Api.DTOs;

namespace ParkEase.Api.Services
{
    public interface IParkingLotService
    {
        // Management views: Admins see every lot, Operators only the lots they own.
        Task<List<ParkingLotDto>> GetLotsAsync(Actor actor);
        Task<ParkingLotDto?> GetLotByIdAsync(int lotId, Actor actor);
        Task<ParkingLotDto> CreateLotAsync(CreateParkingLotDto dto, int ownerId);
        Task<ServiceResult<bool>> DeleteLotAsync(int lotId, Actor actor);

        // Driver-facing: every lot, unfiltered, for finding somewhere to park.
        Task<List<ParkingLotDto>> GetAllLotsForBrowsingAsync();

        Task<List<ParkingSlotDto>> GetSlotsForLotAsync(int lotId);
        Task<ServiceResult<ParkingSlotDto>> CreateSlotAsync(int lotId, CreateParkingSlotDto dto, Actor actor);
        Task<ServiceResult<ParkingSlotDto>> UpdateSlotStatusAsync(int lotId, int slotId, string status, Actor actor);
        Task<ServiceResult<bool>> DeleteSlotAsync(int lotId, int slotId, Actor actor);
    }
}
