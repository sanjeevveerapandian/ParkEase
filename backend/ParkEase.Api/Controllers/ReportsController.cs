using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkEase.Api.DTOs;
using ParkEase.Api.Services;

namespace ParkEase.Api.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(Roles = "Admin")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _service;

        public ReportsController(IReportService service)
        {
            _service = service;
        }

        /// <summary>
        /// Admin-only system-wide report: occupancy rate, total revenue, and
        /// lot-by-lot utilization breakdown.
        /// </summary>
        [HttpGet("overview")]
        [ProducesResponseType(typeof(ReportOverviewDto), 200)]
        public async Task<IActionResult> GetOverview()
        {
            var report = await _service.GetOverviewAsync();
            return Ok(report);
        }
    }
}