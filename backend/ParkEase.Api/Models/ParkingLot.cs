namespace ParkEase.Api.Models
{
    public class ParkingLot
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int OperatorId { get; set; }
        public User? Operator { get; set; }

        public ICollection<ParkingSlot> Slots { get; set; } = new List<ParkingSlot>();
    }
}