using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkEase.Api.Authorization;
using ParkEase.Api.DTOs;
using ParkEase.Api.Services.Notifications;

namespace ParkEase.Api.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize(Policy = AuthPolicies.DriverOnly)]
    public class NotificationsController : ApiControllerBase
    {
        private readonly INotificationInboxService _inbox;

        public NotificationsController(INotificationInboxService inbox)
        {
            _inbox = inbox;
        }

        /// <summary>
        /// The SMS and email messages sent to the calling driver, newest first, with their
        /// delivery status (Sent, Simulated when no SMS gateway is configured, or Failed with the reason).
        /// </summary>
        [HttpGet("mine")]
        [ProducesResponseType(typeof(List<NotificationDto>), 200)]
        public async Task<IActionResult> GetMine()
        {
            return Ok(await _inbox.GetForDriverAsync(GetUserId()));
        }
    }
}
