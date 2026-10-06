namespace ParkEase.Api.Services
{
    public enum ServiceErrorCode
    {
        NotFound,
        Forbidden,
        Conflict,
        Validation
    }

    public record ServiceError(ServiceErrorCode Code, string Message);

    // Lets services say *why* something failed (so the API can answer 404/403/409/400 with a
    // useful message) instead of returning a bare null.
    public class ServiceResult<T>
    {
        public T? Value { get; }
        public ServiceError? Error { get; }
        public bool Succeeded => Error is null;

        private ServiceResult(T? value, ServiceError? error)
        {
            Value = value;
            Error = error;
        }

        public static ServiceResult<T> Ok(T value) => new(value, null);

        public static ServiceResult<T> Fail(ServiceErrorCode code, string message) =>
            new(default, new ServiceError(code, message));
    }
}
