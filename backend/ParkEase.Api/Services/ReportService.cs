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

            // Revenue per lot in one grouped query instead of one query per lot.
            var revenueByLot = await _context.VehicleBookings
                .Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.Overstayed)
                .GroupBy(b => b.ParkingSlot!.ParkingLotId)
                .Select(g => new { LotId = g.Key, Revenue = g.Sum(b => (b.TotalFee ?? 0) + (b.PenaltyFee ?? 0)) })
                .ToDictionaryAsync(x => x.LotId, x => x.Revenue);

            var lotBreakdown = lots.Select(lot =>
            {
                var total = lot.Slots.Count;
                var occupied = lot.Slots.Count(s => s.Status == SlotStatus.Occupied);
                var reserved = lot.Slots.Count(s => s.Status == SlotStatus.Reserved);
                return new LotUtilizationDto
                {
                    LotId = lot.Id,
                    LotName = lot.Name,
                    TotalSlots = total,
                    OccupiedSlots = occupied,
                    ReservedSlots = reserved,
                    OccupancyRatePercent = total == 0 ? 0 : Math.Round((decimal)occupied / total * 100, 1),
                    Revenue = revenueByLot.GetValueOrDefault(lot.Id)
                };
            }).ToList();

            var totalSlots = lotBreakdown.Sum(l => l.TotalSlots);
            var occupiedSlots = lotBreakdown.Sum(l => l.OccupiedSlots);
            var reservedSlots = lotBreakdown.Sum(l => l.ReservedSlots);
            var availableSlots = lots.Sum(l => l.Slots.Count(s => s.Status == SlotStatus.Available));

            return new ReportOverviewDto
            {
                TotalLots = lots.Count,
                TotalSlots = totalSlots,
                OccupiedSlots = occupiedSlots,
                ReservedSlots = reservedSlots,
                AvailableSlots = availableSlots,
                OverallOccupancyRatePercent = totalSlots == 0 ? 0 : Math.Round((decimal)occupiedSlots / totalSlots * 100, 1),
                TotalRevenue = lotBreakdown.Sum(l => l.Revenue),
                LotBreakdown = lotBreakdown
            };
        }
    }
}
