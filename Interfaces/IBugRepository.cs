using JiraDashboard.Dtos;

namespace JiraDashboard;

public interface IBugRepository
{
    Task<List<BugDailyTrendDto>> GetBugDailyTrendAsync();
    Task<BugKpiDto> GetBugStatus();
    Task<List<BugRejectCycleDto>> GetRejectedBugCycleAsync(bool unassigned , int top );
    Task<BugTablePagedResult<BugTableDto>> GetAllBugsTableAsync(string? statusFilter, string sortBy, bool sortDescending, int page, int pageSize);
}