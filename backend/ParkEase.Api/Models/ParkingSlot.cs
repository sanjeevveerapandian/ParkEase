namespace ParkEase.Api.Models
{
    public enum SlotStatus
    {
        Available = 0,
        Occupied = 1,    // a vehicle is parked here (booking is Active)
        Reserved = 2,    // held for a driver who hasn't checked in yet (booking is Reserved)
        Maintenance = 3  // taken out of service by the operator
    }

    public class ParkingSlot
    {
        public int Id { get; set; }
        public string SlotNumber { get; set; } = string.Empty;
        public SlotStatus Status { get; set; } = SlotStatus.Available;
        public decimal HourlyRate { get; set; }

        // What kind of vehicle this bay is built for. Bookings only map a vehicle to a
        // slot of the same type.
        public VehicleType VehicleType { get; set; } = VehicleType.Car;

        public int ParkingLotId { get; set; }
        public ParkingLot? ParkingLot { get; set; }

        public ICollection<VehicleBooking> Bookings { get; set; } = new List<VehicleBooking>();
    }
}
