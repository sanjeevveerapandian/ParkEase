namespace ParkEase.Api.Models
{
    public enum SlotStatus
    {
        Available,
        Occupied,
        Reserved,
        Maintenance
    }

    public class ParkingSlot
    {
        public int Id { get; set; }
        public string SlotNumber { get; set; } = string.Empty;
        public SlotStatus Status { get; set; } = SlotStatus.Available;
        public decimal HourlyRate { get; set; }
        public int ParkingLotId { get; set; }
        public ParkingLot? ParkingLot { get; set; }

        public ICollection<VehicleBooking> Bookings { get; set; } = new List<VehicleBooking>();
    }
}