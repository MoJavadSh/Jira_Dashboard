using JiraDashboard.Dtos;
using JiraDashboard.Helpers;

namespace JiraDashboard;

public interface IJiraRepository
{
    Task<List<UserBarChartDto>> GetUserBatChartAsync(bool unAssigned);
    Task<List<UserIssueCountDto>> GetUserIssueCountAsync(bool unAssigned);
    Task<List<IssueTypeCountDto>> GetIssueTypeCountAsync(bool unAssigned);
    Task<List<IssueTypeProgressDto>> GetIssueTypeProgressAsync(string issueType, bool unAssigned);
}