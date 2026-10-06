using Microsoft.Extensions.Options;

namespace ParkEase.Api.Services
{
    // Runs the two time-driven parts of the booking lifecycle: releasing reservations the driver
    // never checked in for, and warning drivers who are close to the overstay limit.
    public class BookingMaintenanceWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly BookingOptions _options;
        private readonly ILogger<BookingMaintenanceWorker> _logger;

        public BookingMaintenanceWorker(
            IServiceScopeFactory scopes,
            IOptions<BookingOptions> options,
            ILogger<BookingMaintenanceWorker> logger)
        {
            _scopes = scopes;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromSeconds(Math.Max(5, _options.MaintenanceIntervalSeconds));
            using var timer = new PeriodicTimer(interval);

            do
            {
                try
                {
                    // A fresh scope per run: services (and their DbContext) are scoped, not singletons.
                    using var scope = _scopes.CreateScope();
                    var bookings = scope.ServiceProvider.GetRequiredService<IBookingService>();

                    var expired = await bookings.ExpireStaleReservationsAsync();
                    var warned = await bookings.SendOverstayWarningsAsync();

                    if (expired + warned > 0)
                        _logger.LogInformation("Booking upkeep: expired {Expired} reservation(s), sent {Warned} overstay warning(s).", expired, warned);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Booking upkeep run failed; will retry on the next tick.");
                }
            }
            while (await WaitAsync(timer, stoppingToken));
        }

        private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken ct)
        {
            try
            {
                return await timer.WaitForNextTickAsync(ct);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }
    }
}
