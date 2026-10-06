using ParkEase.Api.Models;

namespace ParkEase.Api.Services
{
    // Who is performing an action: taken from the JWT, never from the request body.
    public readonly record struct Actor(int UserId, UserRole Role)
    {
        public bool IsDriver => Role == UserRole.Driver;
        public bool IsOperator => Role == UserRole.Operator;
        public bool IsAdmin => Role == UserRole.Admin;
    }
}
