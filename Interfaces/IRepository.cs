using JiraDashboard.Models;

namespace JiraDashboard;

public interface IRepository
{
        Task<List<JiraIssue>> GetIssuesAsync(IQueryable<JiraIssue>? query = null);
        Task<List<ChangeItem>> GetChangeItemsAsync(List<long> issueIds, string? field = null, string? newValue = null);
        Task<List<IssueType>> GetIssueTypesAsync(string? nameFilter = null);
        Task<List<IssueStatus>> GetIssueStatusesAsync();
        Task<List<AppUser>> GetAppUsersAsync(List<string> userKeys);
        Task<List<Label>> GetLabelsAsync(List<long> issueIds);
        Task<List<ProjectKey>> GetProjectKeysAsync(List<long> projectIds);
        Task<List<IssueLink>> GetIssueLinksAsync(List<long> sourceIds, int linkType);
        IQueryable<JiraIssue> GetIssueQuery();
}