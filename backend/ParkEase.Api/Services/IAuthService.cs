using ParkEase.Api.DTOs;

namespace ParkEase.Api.Services
{
    public interface IAuthService
    {
        Task<ServiceResult<AuthResponseDto>> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    }
}