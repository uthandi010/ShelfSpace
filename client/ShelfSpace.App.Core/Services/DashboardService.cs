using ShelfSpace.App.Core.Models;

namespace ShelfSpace.App.Core.Services;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync();
}

public class DashboardService : IDashboardService
{
    private readonly ApiClient _apiClient;

    public DashboardService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<DashboardSummary> GetSummaryAsync() => _apiClient.GetAsync<DashboardSummary>("/api/dashboard/summary");
}
