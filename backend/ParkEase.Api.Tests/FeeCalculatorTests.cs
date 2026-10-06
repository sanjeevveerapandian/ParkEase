using ParkEase.Api.Services;

namespace ParkEase.Api.Tests
{
    public class FeeCalculatorTests
    {
        private static readonly DateTime Entry = new(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void A_minute_still_bills_one_full_hour()
        {
            var fee = FeeCalculator.Calculate(Entry, Entry.AddMinutes(1), 50m);

            Assert.Equal(1, fee.BillableHours);
            Assert.Equal(50m, fee.TotalFee);
            Assert.Equal(0m, fee.PenaltyFee);
            Assert.False(fee.Overstayed);
        }

        [Fact]
        public void Zero_or_negative_duration_bills_the_one_hour_minimum()
        {
            Assert.Equal(1, FeeCalculator.Calculate(Entry, Entry, 50m).BillableHours);
            Assert.Equal(1, FeeCalculator.Calculate(Entry, Entry.AddMinutes(-30), 50m).BillableHours);
        }

        [Theory]
        [InlineData(60, 1)]    // exactly one hour
        [InlineData(61, 2)]    // 61 minutes rounds up, never 1.02 hours
        [InlineData(120, 2)]
        [InlineData(121, 3)]
        public void Hours_always_round_up(int minutes, int expectedHours)
        {
            var fee = FeeCalculator.Calculate(Entry, Entry.AddMinutes(minutes), 10m);

            Assert.Equal(expectedHours, fee.BillableHours);
            Assert.Equal(expectedHours * 10m, fee.TotalFee);
        }

        [Fact]
        public void Exactly_24_hours_is_not_yet_an_overstay()
        {
            var fee = FeeCalculator.Calculate(Entry, Entry.AddHours(24), 50m);

            Assert.Equal(24, fee.BillableHours);
            Assert.Equal(1200m, fee.TotalFee);
            Assert.Equal(0m, fee.PenaltyFee);
            Assert.False(fee.Overstayed);
        }

        [Fact]
        public void Overstay_hours_cost_one_and_a_half_times_the_rate()
        {
            // 26h at Rs.50: 24 normal hours + 2 overstay hours at 150%.
            var fee = FeeCalculator.Calculate(Entry, Entry.AddHours(26), 50m);

            Assert.Equal(26, fee.BillableHours);
            Assert.Equal(1200m, fee.TotalFee);   // 24 * 50
            Assert.Equal(150m, fee.PenaltyFee);  // 2 * 50 * 1.5
            Assert.True(fee.Overstayed);
        }

        [Fact]
        public void One_minute_past_24_hours_starts_the_overstay()
        {
            var fee = FeeCalculator.Calculate(Entry, Entry.AddHours(24).AddMinutes(1), 40m);

            Assert.Equal(25, fee.BillableHours);
            Assert.Equal(960m, fee.TotalFee);
            Assert.Equal(60m, fee.PenaltyFee);   // 1 * 40 * 1.5
            Assert.True(fee.Overstayed);
        }
    }
}
