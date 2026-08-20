using ParkEase.Api.DTOs;

namespace ParkEase.Api.Services
{
    public interface IBookingService
    {
        Task<BookingDto?> CreateBookingAsync(CreateBookingDto dto, int driverId);
        Task<BookingDto?> ExitBookingAsync(int bookingId, int requestingUserId, bool canExitAnyBooking);
        Task<List<BookingDto>> GetMyBookingsAsync(int driverId);
        Task<List<BookingDto>> GetBookingsForLotAsync(int lotId, int requestingUserId, bool isAdmin);
    }
}