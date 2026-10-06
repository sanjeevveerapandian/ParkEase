using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ParkEase.Api.Models;
using ParkEase.Api.Services;

namespace ParkEase.Api.Controllers
{
    public abstract class ApiControllerBase : ControllerBase
    {
        protected int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);

        protected Actor GetActor()
        {
            var role = User.IsInRole("Admin") ? UserRole.Admin
                     : User.IsInRole("Operator") ? UserRole.Operator
                     : UserRole.Driver;
            return new Actor(GetUserId(), role);
        }

        // Maps a failed ServiceResult to its HTTP status, keeping the { message } error shape
        // the frontend already reads.
        protected IActionResult ErrorResult<T>(ServiceResult<T> result)
        {
            var error = result.Error!;
            var body = new { message = error.Message };
            return error.Code switch
            {
                ServiceErrorCode.NotFound => NotFound(body),
                ServiceErrorCode.Forbidden => StatusCode(StatusCodes.Status403Forbidden, body),
                ServiceErrorCode.Conflict => Conflict(body),
                _ => BadRequest(body)
            };
        }
    }
}
