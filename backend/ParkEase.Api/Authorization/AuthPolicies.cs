namespace ParkEase.Api.Authorization
{

    public static class AuthPolicies
    {
        public const string DriverOnly = "DriverOnly";
        public const string OperatorOnly = "OperatorOnly";
        public const string AdminOnly = "AdminOnly";
        public const string OperatorOrAdmin = "OperatorOrAdmin";
        public const string AnyAuthenticatedUser = "AnyAuthenticatedUser";
    }
}
