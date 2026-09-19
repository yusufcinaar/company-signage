using CompanySignage.Application.DTOs.Dashboard;

namespace CompanySignage.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync();
}
