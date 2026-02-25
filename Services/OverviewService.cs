using JiraDashboard.Dtos;
using JiraDashboard.Repository;
using Microsoft.EntityFrameworkCore;

namespace JiraDashboard.Services;

public class OverviewService : IOverviewService
{
    private readonly IRepository _repo;

    private static readonly HashSet<string> ClosedStatusNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Done", "Resolved", "Resolve"
    };

    public OverviewService(IRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<UserBarChartDto>> GetUserBarChartAsync(bool unAssigned)
    {
        var query = _repo.GetIssueQuery()
            .Where(t => t.IssueTypeObj.PName != "Story");

        if (unAssigned)
            query = query.Where(x => x.AppUser != null && x.AppUser.User != null);

        var raw = await query
            .Select(task => new
            {
                AssigneeName  = task.AppUser != null && task.AppUser.User != null
                    ? task.AppUser.User.DisplayName
                    : "Un Assigned",
                IssueTypeName = task.IssueTypeObj.PName
            })
            .GroupBy(x => new { x.AssigneeName, x.IssueTypeName })
            .Select(g => new
            {
                g.Key.AssigneeName,
                g.Key.IssueTypeName,
                Count = g.Count()
            })
            .ToListAsync();

        var allUsers      = raw.Select(x => x.AssigneeName).Distinct().ToList();
        var allIssueTypes = raw.Select(x => x.IssueTypeName).Distinct().ToList();

        return allUsers.Select(user => new UserBarChartDto
        {
            AssigneeName = user,
            IssueTypes   = allIssueTypes.Select(issueType => new IssueTypeCountDto
            {
                IssueTypeName = issueType,
                Count         = raw
                    .Where(r => r.AssigneeName == user && r.IssueTypeName == issueType)
                    .Select(r => r.Count)
                    .FirstOrDefault(),
                Percentage = null
            }).ToList()
        }).ToList();
    }

    public async Task<List<UserIssueCountDto>> GetUserIssueCountAsync(bool unAssigned)
    {
        var query = _repo.GetIssueQuery()
            .Where(task => task.IssueTypeObj.PName != "Story");

        if (unAssigned)
            query = query.Where(t => t.AppUser != null && t.AppUser.User != null);

        var grouped = await query
            .GroupBy(task =>
                task.AppUser != null && task.AppUser.User != null
                    ? task.AppUser.User.DisplayName
                    : "Un Assigned")
            .Select(g => new
            {
                AssigneeName = g.Key,
                Count        = g.Count()
            })
            .ToListAsync();

        var totalIssues = grouped.Sum(r => r.Count);

        return grouped.Select(r => new UserIssueCountDto
        {
            AssigneeName = r.AssigneeName,
            IssueCount   = r.Count,
            Percentage   = totalIssues > 0
                ? Math.Round((double)r.Count / totalIssues * 100, 1)
                : 0,
        }).ToList();
    }

    public async Task<List<IssueTypeCountDto>> GetIssueTypeCountAsync(bool unAssigned)
    {
        var query = _repo.GetIssueQuery()
            .Where(task => task.IssueTypeObj.PName != "Story");

        if (unAssigned)
            query = query.Where(t => t.AppUser != null && t.AppUser.User != null);

        var grouped = await query
            .GroupBy(t => t.IssueTypeObj.PName)
            .Select(g => new
            {
                IssueTypeName = g.Key,
                Count         = g.Count()
            })
            .ToListAsync();

        var totalIssues = grouped.Sum(r => r.Count);

        return grouped.Select(r => new IssueTypeCountDto
        {
            IssueTypeName = r.IssueTypeName,
            Count         = r.Count,
            Percentage    = totalIssues > 0
                ? Math.Round((double)r.Count / totalIssues * 100, 1)
                : 0
        }).ToList();
    }

    public async Task<List<IssueTypeProgressDto>> GetIssueTypeProgressAsync(string issueType, bool unAssigned)
    {
        var query = _repo.GetIssueQuery()
            .Where(task => task.IssueTypeObj.PName != "Story");

        if (!string.IsNullOrWhiteSpace(issueType))
            query = query.Where(t => t.IssueTypeObj.PName == issueType);

        if (unAssigned)
            query = query.Where(t => t.AppUser != null && t.AppUser.User != null);

        var raw = await query
            .Select(t => new
            {
                IssueTypeName = t.IssueTypeObj.PName,
                StatusName    = t.IssueStatusObj.PName
            })
            .GroupBy(x => new { x.IssueTypeName, x.StatusName })
            .Select(g => new
            {
                g.Key.IssueTypeName,
                g.Key.StatusName,
                Count = g.Count()
            })
            .ToListAsync();

        return raw
            .GroupBy(x => x.IssueTypeName)
            .Select(g =>
            {
                var total = g.Sum(x => x.Count);
                return new IssueTypeProgressDto
                {
                    IssueTypeName = g.Key,
                    Statuses      = g.Select(x => new StatusCountDto
                    {
                        StatusName = x.StatusName,
                        Count      = x.Count,
                        Percentage = total > 0
                            ? Math.Round((double)x.Count / total * 100, 1)
                            : 0
                    }).ToList()
                };
            })
            .ToList();
    }

    public async Task<OpenClosedDto> GetOpenClosedAsync()
    {
        var issues = await _repo.GetIssueQuery()
            .Where(task => task.IssueTypeObj.PName != "Story")
            .Select(t => new
            {
                t.Assignee,
                StatusName = t.IssueStatusObj.PName
            })
            .ToListAsync();

        var assignedIssues  = issues.Where(t => !string.IsNullOrWhiteSpace(t.Assignee)).ToList();
        var unassignedCount = issues.Count - assignedIssues.Count;
        var openCount       = assignedIssues.Count(t => !ClosedStatusNames.Contains(t.StatusName));
        var closedCount     = assignedIssues.Count(t => ClosedStatusNames.Contains(t.StatusName));

        return new OpenClosedDto
        {
            Open       = openCount,
            Closed     = closedCount,
            Unassigned = unassignedCount
        };
    }
}