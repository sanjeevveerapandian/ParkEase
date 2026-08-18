namespace ParkEase.Api.DTOs
{
    // Deliberately has NO OperatorId field — the operator is taken from the
    // logged-in user's JWT token server-side, never trusted from client input.
    // This prevents an Operator from creating a lot and assigning it to someone else.
    public class CreateParkingLotDto
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }
}