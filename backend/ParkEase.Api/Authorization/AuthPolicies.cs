namespace ParkEase.Api.Authorization
{
    // Named authorization policies, one per distinct access level this API exposes.
    // Every action should declare its access level explicitly via one of these —
    // no endpoint should rely on "no [Authorize] attribute present" to silently mean
    // "open to any authenticated role". That exact ambiguity (an attribute's presence
    // vs. absence deciding access) previously let a class-level role restriction
    // silently block an action meant to be more open; AnyAuthenticatedUser exists so
    // "intentionally open" endpoints declare that on purpose, the same way restricted
    // ones declare their restriction.
    public static class AuthPolicies
    {
        public const string DriverOnly = "DriverOnly";
        public const string OperatorOnly = "OperatorOnly";
        public const string AdminOnly = "AdminOnly";
        public const string OperatorOrAdmin = "OperatorOrAdmin";
        public const string AnyAuthenticatedUser = "AnyAuthenticatedUser";
    }
}
