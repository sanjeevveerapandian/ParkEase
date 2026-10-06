using ParkEase.Api.DTOs;

namespace ParkEase.Api.Services
{
    public interface IBookingService
    {
        // Reserved --> Active --> Completed | Overstayed, with Cancelled / Expired as side exits.
        Task<ServiceResult<BookingDto>> ReserveAsync(CreateBookingDto dto, int driverId);
        Task<ServiceResult<BookingDto>> CheckInAsync(int bookingId, Actor actor);
        Task<ServiceResult<BookingDto>> ExitAsync(int bookingId, Actor actor);
        Task<ServiceResult<BookingDto>> CancelAsync(int bookingId, Actor actor);

        Task<List<BookingDto>> GetMyBookingsAsync(int driverId);
        Task<List<BookingDto>> GetBookingsForLotAsync(int lotId, Actor actor);

        // Run on a timer by BookingMaintenanceWorker; each returns how many bookings it touched.
        Task<int> ExpireStaleReservationsAsync();
        Task<int> SendOverstayWarningsAsync();
    }
}
