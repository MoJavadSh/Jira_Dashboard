using JiraDashboard.Dtos;
using JiraDashboard.Helpers;

namespace JiraDashboard;

public interface IJiraRepository
{
    Task<List<BugTableDto>> GetAllIssueAsync();
}