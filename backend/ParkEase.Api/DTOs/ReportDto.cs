namespace ParkEase.Api.DTOs
{
    public class LotUtilizationDto
    {
        public int LotId { get; set; }
        public string LotName { get; set; } = string.Empty;
        public int TotalSlots { get; set; }
        public int OccupiedSlots { get; set; }
        public decimal OccupancyRatePercent { get; set; }
        public decimal Revenue { get; set; }
    }

    public class ReportOverviewDto
    {
        public int TotalLots { get; set; }
        public int TotalSlots { get; set; }
        public int OccupiedSlots { get; set; }
        public int AvailableSlots { get; set; }
        public decimal OverallOccupancyRatePercent { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<LotUtilizationDto> LotBreakdown { get; set; } = new();
    }
}