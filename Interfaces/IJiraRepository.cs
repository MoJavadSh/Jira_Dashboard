using JiraDashboard.Dtos;
using JiraDashboard.Helpers;

namespace JiraDashboard;

public interface IJiraRepository
{
    Task<List<BugTableDto>> GetAllIssueAsync(
        string? assignee = null,
        string? issueType = null,
        string? progress = null,
        string? keyContains = null,
        DateTime? createdDate = null,
        DateTime? closedDate = null
        
    );
}