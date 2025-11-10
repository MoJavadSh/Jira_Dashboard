using JiraDashboard.Dtos;

namespace JiraDashboard;

public interface IBugRepository
{
    Task<List<BugDailyTrendDto>> GetBugDailyTrendAsync();
    Task<BugKpiDto> GetBugStatus();
}