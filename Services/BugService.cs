using JiraDashboard.Dtos;
using JiraDashboard.Repository;
using Microsoft.EntityFrameworkCore;

namespace JiraDashboard.Services;

public class BugService : IBugService
{
    private readonly IRepository _repo;
    private const string DoneStatusId = "10001";
    private const string ToDoStatusId = "10000";

    public BugService(IRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<BugDailyTrendDto>> GetBugDailyTrendAsync()
    {
        var bugs = await _repo.GetIssueQuery()
            .Where(t => t.IssueTypeObj.PName == "bug")
            .Select(t => new { t.Id, t.Created })
            .ToListAsync();

        if (!bugs.Any()) return new List<BugDailyTrendDto>();

        var bugIds = bugs.Select(b => b.Id).ToList();
        var changeItems = await _repo.GetChangeItemsAsync(bugIds, field: "status", newValue: DoneStatusId);

        var closedChanges = changeItems
            .Select(ci => new
            {
                ci.ChangeGroup.IssueId,
                ClosedDate = ci.ChangeGroup.Created
            })
            .ToList();

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
            Date    = date,
            Created = createdByDate.FirstOrDefault(x => x.Date == date)?.Created ?? 0,
            Closed  = closedByDate.FirstOrDefault(x => x.Date == date)?.Closed ?? 0
        }).ToList();
    }

    public async Task<BugKpiDto> GetBugStatusAsync()
    {
        var bugTypes   = await _repo.GetIssueTypesAsync("bug");
        var bugTypeIds = bugTypes.Select(t => t.Id).ToList();

        if (!bugTypeIds.Any()) return new BugKpiDto();

        var allBugs = await _repo.GetIssueQuery()
            .Where(t => bugTypeIds.Contains(t.IssueType))
            .Select(t => new { t.Id, t.IssueStatus })
            .ToListAsync();

        var bugIds      = allBugs.Select(b => b.Id).ToList();
        var changeItems = await _repo.GetChangeItemsAsync(bugIds, field: "status");

        var statusChanges = changeItems
            .Where(ci => ci.NewValue == ToDoStatusId || ci.NewValue == DoneStatusId)
            .Select(ci => new
            {
                ci.ChangeGroup.IssueId,
                ci.NewValue,
                ci.ChangeGroup.Created
            })
            .ToList();

        var bugCycles  = statusChanges
            .GroupBy(c => c.IssueId)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.Created).ToList());

        var cycleTimes = new List<double>();

        foreach (var kvp in bugCycles)
        {
            var changes  = kvp.Value;
            var toDoTime = changes.FirstOrDefault(c => c.NewValue == ToDoStatusId)?.Created;
            var doneTime = changes.FirstOrDefault(c => c.NewValue == DoneStatusId)?.Created;

            if (toDoTime.HasValue && doneTime.HasValue && doneTime > toDoTime)
                cycleTimes.Add((doneTime.Value - toDoTime.Value).TotalDays);
        }

        var totalBugs       = allBugs.Count;
        var currentlyClosed = allBugs.Count(b => b.IssueStatus == DoneStatusId);
        var avgCycleTime    = cycleTimes.Any() ? cycleTimes.Average() : 0;

        return new BugKpiDto
        {
            TotalBugs            = totalBugs,
            OpenBugs             = totalBugs - currentlyClosed,
            ClosedBugs           = currentlyClosed,
            AvgTimeToCloseInDays = Math.Round(avgCycleTime, 2),
        };
    }

    public async Task<List<BugRejectCycleDto>> GetRejectedBugCycleAsync(bool unassigned, int top)
    {
        var query = _repo.GetIssueQuery()
            .Where(j => j.IssueTypeObj.PName.ToLower().Contains("bug"))
            .Where(j => j.ProjectId != null && j.IssueNum != null);

        if (unassigned)
            query = query.Where(j => j.Assignee != null!);

        var rawIssues = await query
            .Select(j => new
            {
                j.Id,
                j.IssueNum,
                j.Summary,
                AssigneeName = j.AppUser != null && j.AppUser.User != null
                    ? j.AppUser.User.DisplayName
                    : "Unassigned"
            })
            .Take(500)
            .ToListAsync();

        if (!rawIssues.Any()) return new List<BugRejectCycleDto>();

        var issueIds     = rawIssues.Select(x => x.Id).ToList();
        var rejectedItems = await _repo.GetChangeItemsAsync(issueIds, field: "status", newString: "Rejected");

        var rejectedDict = rejectedItems
            .GroupBy(ci => ci.ChangeGroup.IssueId)
            .ToDictionary(g => g.Key, g => g.Count());

        return rawIssues
            .Select(issue => new
            {
                IssueKey    = issue.IssueNum,
                ReopenCount = rejectedDict.GetValueOrDefault(issue.Id, 0),
                issue.Summary,
                issue.AssigneeName
            })
            .Where(x => x.ReopenCount > 0)
            .OrderByDescending(x => x.ReopenCount)
            .ThenByDescending(x => x.IssueKey)
            .Take(top)
            .Select(x => new BugRejectCycleDto
            {
                IssueKey    = x.IssueKey,
                ReopenCount = x.ReopenCount,
                Summary     = string.IsNullOrEmpty(x.Summary) ? "" : x.Summary,
                Assignee    = x.AssigneeName
            })
            .ToList();
    }

    public async Task<List<BugTableDto>> GetAllBugsTableAsync(
        string? statusFilter,
        string sortBy,
        bool sortDescending,
        int page,
        int pageSize)
    {
        var query = _repo.GetIssueQuery()
            .Where(j => j.IssueTypeObj.PName.ToLower().Contains("bug"))
            .Where(j => j.ProjectId != null && j.IssueNum != null);

        if (!string.IsNullOrWhiteSpace(statusFilter))
            query = query.Where(j => j.IssueStatusObj.PName == statusFilter);

        query = sortBy.ToLower() switch
        {
            "created" => sortDescending
                ? query.OrderByDescending(j => j.Created)
                : query.OrderBy(j => j.Created),
            _ => sortDescending
                ? query.OrderByDescending(j => j.IssueNum)
                : query.OrderBy(j => j.IssueNum)
        };

        var items = await query
            .Select(j => new
            {
                j.Id,
                j.IssueNum,
                j.Summary,
                j.Creator,
                j.Created,
                StatusName   = j.IssueStatusObj.PName,
                AssigneeName = j.AppUser != null && j.AppUser.User != null
                    ? j.AppUser.User.DisplayName
                    : "Unassigned"
            })
            .ToListAsync();

        if (!items.Any()) return new List<BugTableDto>();

        var issueIds    = items.Select(x => x.Id).ToList();
        var creatorKeys = items
            .Where(x => !string.IsNullOrEmpty(x.Creator))
            .Select(x => x.Creator!)
            .Distinct()
            .ToList();

        var creatorUsersTask = _repo.GetAppUsersAsync(creatorKeys);
        var labelsTask       = _repo.GetLabelsAsync(issueIds);
        await Task.WhenAll(creatorUsersTask, labelsTask);

        var creatorNames = creatorUsersTask.Result
            .ToDictionary(
                u => u.UserKey,
                u => u.User != null ? u.User.DisplayName : u.UserKey);

        var labelsDict = labelsTask.Result
            .GroupBy(l => l.IssueId)
            .ToDictionary(
                g => g.Key,
                g => string.Join(", ", g.Select(l => l.LabelName).OrderBy(l => l)));

        return items.Select(x => new BugTableDto
        {
            Key         = x.IssueNum,
            Summary     = x.Summary ?? "Empty",
            Progress    = x.StatusName,
            Reporter    = creatorNames.GetValueOrDefault(x.Creator ?? "", "Unknown"),
            Assignee    = x.AssigneeName,
            Labels      = labelsDict.GetValueOrDefault(x.Id, "-"),
            DateCreated = x.Created,
            LifeTime    = DateTime.UtcNow - x.Created
        }).ToList();
    }
}