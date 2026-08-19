using ParkEase.Api.DTOs;

namespace ParkEase.Api.Services
{
    public interface IReportService
    {
        Task<ReportOverviewDto> GetOverviewAsync();
    }
}