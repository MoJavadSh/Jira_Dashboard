using JiraDashboard.Dtos;
using JiraDashboard.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace JiraDashboard;

public interface IJiraRepository
{
    Task<List<BugTableDto>> GetAllIssueAsync(
        string? assignee = null,
        string? issueType = null,
        string? progress = null,
        int? keyContains = null,
        DateTime? createdDate = null,
        DateTime? closedDate = null
        
    );

    Task<JiraMetadataDto> GetJiraMetadata();
}