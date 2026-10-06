namespace ParkEase.Api.Services
{
    public class BookingOptions
    {
        // How long a reservation holds a slot before it is released if the driver hasn't checked in.
        public int ReservationHoldMinutes { get; set; } = 15;

        // How often the background worker expires stale reservations and sends overstay warnings.
        public int MaintenanceIntervalSeconds { get; set; } = 60;
    }
}
