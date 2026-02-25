using JiraDashboard.Dtos;
using JiraDashboard.Models;

namespace JiraDashboard;

public interface IRepository
{
        IQueryable<JiraIssue> GetIssueQuery();

        Task<List<ChangeItem>> GetChangeItemsAsync(
                List<long> issueIds,
                string? field = null,
                string? newValue = null,
                string? newString = null);

        Task<List<IssueType>> GetIssueTypesAsync(string? nameContains = null);

        Task<List<IssueStatus>> GetIssueStatusesAsync();

        Task<List<AppUser>> GetAppUsersAsync(List<string> userKeys);

        Task<List<Label>> GetLabelsAsync(List<long> issueIds);

        Task<List<ProjectKey>> GetProjectKeysAsync(List<long> projectIds);

        Task<List<IssueLink>> GetIssueLinksAsync(List<long> sourceIds, int linkType);

        Task<JiraMetadataDto> GetJiraMetadataAsync();
}
