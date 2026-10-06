namespace ParkEase.Api.Services
{
    public readonly record struct FeeBreakdown(decimal TotalFee, decimal PenaltyFee, int BillableHours, bool Overstayed);

    // Pure billing rules, kept free of EF/HTTP so they can be tested directly.
    public static class FeeCalculator
    {
        public const int OverstayThresholdHours = 24;
        public const decimal OverstayPenaltyMultiplier = 1.5m; // overstay hours cost 150%

        public static FeeBreakdown Calculate(DateTime entryUtc, DateTime exitUtc, decimal hourlyRate)
        {
            var duration = exitUtc > entryUtc ? exitUtc - entryUtc : TimeSpan.Zero;

            // Always round up to the next full hour, minimum 1: 61 minutes bills as 2 hours.
            var billableHours = Math.Max(1, (int)Math.Ceiling(duration.TotalHours));

            if (billableHours > OverstayThresholdHours)
            {
                var overstayHours = billableHours - OverstayThresholdHours;
                return new FeeBreakdown(
                    TotalFee: OverstayThresholdHours * hourlyRate,
                    PenaltyFee: overstayHours * hourlyRate * OverstayPenaltyMultiplier,
                    BillableHours: billableHours,
                    Overstayed: true);
            }

            return new FeeBreakdown(billableHours * hourlyRate, 0, billableHours, false);
        }
    }
}
