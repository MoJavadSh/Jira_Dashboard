using JiraDashboard.Dtos;

namespace JiraDashboard;

public interface IOverviewRepository
{
    Task<List<UserBarChartDto>> GetUserBarChartAsync(bool unAssigned);
    Task<List<UserIssueCountDto>> GetUserIssueCountAsync(bool unAssigned);
    Task<List<IssueTypeCountDto>> GetIssueTypeCountAsync(bool unAssigned);
    Task<List<IssueTypeProgressDto>> GetIssueTypeProgressAsync(string issueType, bool unAssigned);
    Task<OpenClosedDto> GetOpenClosedAsync();
}