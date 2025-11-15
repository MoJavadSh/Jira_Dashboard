using JiraDashboard.Data;
using JiraDashboard.Dtos;
using Microsoft.EntityFrameworkCore;

namespace JiraDashboard.Repository;

public class BugRepository : IBugRepository
{
    private readonly AppDbContext _context;

    public BugRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<BugDailyTrendDto>> GetBugDailyTrendAsync()
    {
        var doneStatusId = "10001";

        var bugs = await _context.JiraIssues.AsNoTracking()
            .Where(t => t.IssueTypeObj.PName == "bug")
            .Where(task => task.IssueTypeObj.PName != "Story")
            .Select(t => new
            {
                t.Id,
                Created = t.Created,
                t.IssueStatusObj.PName
            })
            .ToListAsync();

        if (!bugs.Any()) return new List<BugDailyTrendDto>();

        var bugIds = bugs.Select(b => b.Id).ToList();

        var closedChanges = await _context.ChangeItems.AsNoTracking()
            .Include(ci => ci.ChangeGroup)
            .Where(ci => ci.Field == "status"
                         && doneStatusId.Contains(ci.NewValue)
                         && bugIds.Contains(ci.ChangeGroup.IssueId))
            .Select(ci => new
            {
                ci.ChangeGroup.IssueId,
                ClosedDate = ci.ChangeGroup.Created
            })
            .ToListAsync();

        var createdByDate = bugs
            .GroupBy(b => b.Created.Date)
            .Select(g => new { Date = g.Key, Created = g.Count() })
            .ToList();

        var closedByDate = closedChanges
            .GroupBy(c => c.ClosedDate.Date)
            .Select(g => new { Date = g.Key, Closed = g.Count() })
            .ToList();

        var allDates = createdByDate.Select(x => x.Date)
            .Union(closedByDate.Select(x => x.Date))
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        return allDates.Select(date => new BugDailyTrendDto
        {
            Date = date,
            Created = createdByDate.FirstOrDefault(x => x.Date == date)?.Created ?? 0,
            Closed = closedByDate.FirstOrDefault(x => x.Date == date)?.Closed ?? 0
        }).ToList();
    }

    public async Task<BugKpiDto> GetBugStatus()
    {
        var bugTypeIds = await _context.IssueTypes.AsNoTracking()
            .Where(t => t.PName.ToLower().Contains("bug"))
            .Select(t => t.Id)
            .ToListAsync();

        if (!bugTypeIds.Any()) return new BugKpiDto();

        var toDoStatusId = "10000";
        var doneStatusId = "10001";

        var allBugs = await _context.JiraIssues.AsNoTracking()
            .Where(t => bugTypeIds.Contains(t.IssueType))
            .Select(t => new { t.Id, t.IssueStatus })
            .ToListAsync();

        var totalBugs = allBugs.Count;
        var currentlyClosed = allBugs.Count(b => b.IssueStatus == doneStatusId);

        var statusChanges = await _context.ChangeItems.AsNoTracking()
            .Include(ci => ci.ChangeGroup)
            .Where(ci => ci.Field == "status"
                         && (ci.NewValue == toDoStatusId || ci.NewValue == doneStatusId)
                         && bugTypeIds.Contains(ci.ChangeGroup.JiraIssue.IssueType))
            .Select(ci => new
            {
                ci.ChangeGroup.IssueId,
                ci.NewValue,
                ci.ChangeGroup.Created
            })
            .ToListAsync();

        var bugCycles = statusChanges
            .GroupBy(c => c.IssueId)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.Created).ToList());

        var cycleTimes = new List<double>();
        var closedViaCycle = 0;

        foreach (var kvp in bugCycles)
        {
            var changes = kvp.Value;
            var toDoTime = changes.FirstOrDefault(c => c.NewValue == toDoStatusId)?.Created;
            var doneTime = changes.FirstOrDefault(c => c.NewValue == doneStatusId)?.Created;

            if (toDoTime.HasValue && doneTime.HasValue && doneTime > toDoTime)
            {
                cycleTimes.Add((doneTime.Value - toDoTime.Value).TotalDays);
                closedViaCycle++;
            }
        }

        var avgCycleTime = cycleTimes.Any() ? cycleTimes.Average() : 0;

        return new BugKpiDto
        {
            TotalBugs = totalBugs,
            OpenBugs = totalBugs - currentlyClosed,
            ClosedBugs = currentlyClosed,
            AvgTimeToCloseInDays = Math.Round(avgCycleTime, 2),
        };
    }

    public async Task<List<BugRejectCycleDto>> GetRejectedBugCycleAsync(bool unassigned , int top = 10)
    {
        var bugTypeIds = await _context.IssueTypes
            .AsNoTracking()
            .Where(t => t.PName.ToLower().Contains("bug"))
            .Select(t => t.Id)
            .ToListAsync();

        if (!bugTypeIds.Any())
            return new List<BugRejectCycleDto>();

        var bugsQuery = _context.JiraIssues
            .AsNoTracking()
            .Where(j => bugTypeIds.Contains(j.IssueType))
            .Where(j => j.ProjectId != null && j.IssueNum != null)
            .Select(j => new
            {
                j.Id,
                j.ProjectId,
                j.IssueNum,
                j.Summary,
                j.Assignee
            });

        if (unassigned == true)
            bugsQuery = bugsQuery.Where(j=>j.Assignee != null!);
        else if (unassigned == false)
            bugsQuery = bugsQuery.Where(j => !string.IsNullOrEmpty(j.Assignee));

        var rawBugs = await bugsQuery.Take(500).ToListAsync(); // حداکثر 500 تا برای عملکرد خوب

        if (!rawBugs.Any())
            return new List<BugRejectCycleDto>();

        var bugIds = rawBugs.Select(b => b.Id).ToList();

        var rejectedCounts = await _context.ChangeItems
            .AsNoTracking()
            .Include(ci => ci.ChangeGroup)
            .Where(ci => ci.Field == "status"
                         && ci.NewString == "Rejected"
                         && bugIds.Contains(ci.ChangeGroup.IssueId))
            .GroupBy(ci => ci.ChangeGroup.IssueId)
            .Select(g => new
            {
                IssueId = g.Key,
                ReopenCount = g.Count()
            })
            .ToListAsync();

        var rejectedDict = rejectedCounts.ToDictionary(x => x.IssueId, x => x.ReopenCount);

        var projectIds = rawBugs.Select(b => b.ProjectId!.Value).Distinct().ToList();
        var projectKeyDict = await _context.ProjectKeys
            .AsNoTracking()
            .Where(pk => projectIds.Contains(pk.ProjectId))
            .ToDictionaryAsync(pk => pk.ProjectId, pk => pk.ProjectKeyName);

        var assigneeKeys = rawBugs
            .Where(b => !string.IsNullOrEmpty(b.Assignee))
            .Select(b => b.Assignee!)
            .Distinct()
            .ToList();

        var assigneeNames = await _context.AppUsers
            .AsNoTracking()
            .Where(u => assigneeKeys.Contains(u.UserKey))
            .ToDictionaryAsync(
                u => u.UserKey,
                u => u.User?.DisplayName ?? u.UserKey
            );

        var result = rawBugs
            .Select(b =>
            {
                var reopenCount = rejectedDict.GetValueOrDefault(b.Id, 0);
                var projectKey = projectKeyDict.GetValueOrDefault(b.ProjectId!.Value, "UNKNOWN");
                var issueKey = $"{projectKey}-{b.IssueNum}";

                return new BugRejectCycleDto
                {
                    IssueKey = issueKey,
                    ReopenCount = reopenCount,
                    Summary = string.IsNullOrEmpty(b.Summary) ? "(بدون خلاصه)" : b.Summary,
                    Assignee = string.IsNullOrEmpty(b.Assignee)
                        ? "Unassigned"
                        : assigneeNames.GetValueOrDefault(b.Assignee, "Unknown"),
                    Url = $"https://your-jira-domain.com/browse/{issueKey}"
                };
            })
            .Where(x => x.ReopenCount > 0) // فقط اونایی که حداقل 1 بار Rejected شدن
            .OrderByDescending(x => x.ReopenCount)
            .ThenByDescending(x => x.IssueKey)
            .Take(top)
            .ToList();

        return result;
    }
}