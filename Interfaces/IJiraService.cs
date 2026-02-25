using JiraDashboard.Dtos;

namespace JiraDashboard;

public interface IJiraService
{
    Task<List<BugTableDto>> GetAllIssueAsync(
        string? assignee = null,
        string? issueType = null,
        string? progress = null,
        int? keyContains = null,
        DateTime? createdDate = null,
        DateTime? closedDate = null);

    Task<JiraMetadataDto> GetJiraMetadataAsync();
}