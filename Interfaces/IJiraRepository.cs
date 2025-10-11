using JiraDashboard.Dtos;
using JiraDashboard.Helpers;

namespace JiraDashboard;

public interface IJiraRepository
{
    Task<List<UserBarChartDto>> GetUserBatChartAsync();
    Task<List<UserIssueCountDto>> GetUserIssueCountAsync(QueryObject query);
    Task<List<IssueTypeCountDto>> GetIssueTypeCountAsync(QueryObject query);
}