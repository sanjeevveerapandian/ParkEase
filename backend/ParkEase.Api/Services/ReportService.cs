using Microsoft.EntityFrameworkCore;
using ParkEase.Api.Data;
using ParkEase.Api.DTOs;
using ParkEase.Api.Models;

namespace ParkEase.Api.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ReportOverviewDto> GetOverviewAsync()
        {
            var lots = await _context.ParkingLots.Include(l => l.Slots).ToListAsync();

            // Revenue = sum of all completed/overstayed bookings' fee + penalty, across the whole system.
            var totalRevenue = await _context.VehicleBookings
                .Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.Overstayed)
                .SumAsync(b => (b.TotalFee ?? 0) + (b.PenaltyFee ?? 0));

            var lotBreakdown = new List<LotUtilizationDto>();
            int totalSlots = 0, occupiedSlots = 0;

            foreach (var lot in lots)
            {
                var lotTotalSlots = lot.Slots.Count;
                var lotOccupied = lot.Slots.Count(s => s.Status == SlotStatus.Occupied);
                var lotRevenue = await _context.VehicleBookings
                    .Where(b => b.ParkingSlot!.ParkingLotId == lot.Id &&
                                (b.Status == BookingStatus.Completed || b.Status == BookingStatus.Overstayed))
                    .SumAsync(b => (b.TotalFee ?? 0) + (b.PenaltyFee ?? 0));

                lotBreakdown.Add(new LotUtilizationDto
                {
                    LotId = lot.Id,
                    LotName = lot.Name,
                    TotalSlots = lotTotalSlots,
                    OccupiedSlots = lotOccupied,
                    OccupancyRatePercent = lotTotalSlots == 0 ? 0 : Math.Round((decimal)lotOccupied / lotTotalSlots * 100, 1),
                    Revenue = lotRevenue
                });

                totalSlots += lotTotalSlots;
                occupiedSlots += lotOccupied;
            }

            return new ReportOverviewDto
            {
                TotalLots = lots.Count,
                TotalSlots = totalSlots,
                OccupiedSlots = occupiedSlots,
                AvailableSlots = totalSlots - occupiedSlots,
                OverallOccupancyRatePercent = totalSlots == 0 ? 0 : Math.Round((decimal)occupiedSlots / totalSlots * 100, 1),
                TotalRevenue = totalRevenue,
                LotBreakdown = lotBreakdown
            };
        }
    }
}