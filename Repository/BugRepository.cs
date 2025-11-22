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

    public async Task<List<BugRejectCycleDto>> GetRejectedBugCycleAsync(bool unassigned , int top)
    {
    var query = _context.JiraIssues
        .AsNoTracking()
        .Where(j => j.IssueTypeObj.PName.ToLower().Contains("bug"))
        .Where(j => j.ProjectId != null && j.IssueNum != null);

    if (unassigned)
        query = query.Where(j=>j.Assignee != null!);
    
    query = query
        .Include(j => j.AppUser)
        .ThenInclude(u => u.User)
        .Include(j => j.IssueTypeObj);

    var rawIssues = await query
        .Select(j => new
        {
            j.Id,
            j.ProjectId,
            j.IssueNum,
            j.Summary,
            AssigneeName = j.AppUser != null && j.AppUser.User != null 
                ? j.AppUser.User.DisplayName 
                : "Unassigned"
        })
        .Take(500)
        .ToListAsync();

    if (!rawIssues.Any())
        return new List<BugRejectCycleDto>();

    var issueIds = rawIssues.Select(x => x.Id).ToList();

    var rejectedCounts = await _context.ChangeItems
        .AsNoTracking()
        .Include(ci => ci.ChangeGroup)
        .Where(ci => ci.Field == "status" 
                  && ci.NewString == "Rejected"
                  && issueIds.Contains(ci.ChangeGroup.IssueId))
        .GroupBy(ci => ci.ChangeGroup.IssueId)
        .Select(g => new
        {
            IssueId = g.Key,
            ReopenCount = g.Count()
        })
        .ToListAsync();

    var rejectedDict = rejectedCounts.ToDictionary(x => x.IssueId, x => x.ReopenCount);

    var projectIds = rawIssues.Select(x => x.ProjectId!.Value).Distinct().ToList();
    var projectKeys = await _context.ProjectKeys
        .AsNoTracking()
        .Where(pk => projectIds.Contains(pk.ProjectId))
        .ToDictionaryAsync(pk => pk.ProjectId, pk => pk.ProjectKeyName);

    var result = rawIssues
        .Select(issue =>
        {
            var reopenCount = rejectedDict.GetValueOrDefault(issue.Id, 0);
            var projectKey = projectKeys.GetValueOrDefault(issue.ProjectId!.Value, "UNKNOWN");
            var issueKey = $"{projectKey}-{issue.IssueNum}";

            return new
            {
                IssueKey = issueKey,
                ReopenCount = reopenCount,
                issue.Summary,
                issue.AssigneeName
            };
        })
        .Where(x => x.ReopenCount > 0)
        .OrderByDescending(x => x.ReopenCount)
        .ThenByDescending(x => x.IssueKey)
        .Take(top)
        .Select(x => new BugRejectCycleDto
        {
            IssueKey = x.IssueKey,
            ReopenCount = x.ReopenCount,
            Summary = string.IsNullOrEmpty(x.Summary) ? "" : x.Summary,
            Assignee = x.AssigneeName
        })
        .ToList();

    return result;
    }
    
    public async Task<List<BugTableDto>> GetAllBugsTableAsync(
    string? statusFilter,
    string sortBy,
    bool sortDescending,
    int page,
    int pageSize)
{
    var query = _context.JiraIssues
        .AsNoTracking()
        .Include(j => j.IssueStatusObj)
        .Include(j => j.IssueTypeObj)
        .Include(j => j.AppUser).ThenInclude(u => u.User)
        .Where(j => j.IssueTypeObj.PName.ToLower().Contains("bug"))
        .Where(j => j.ProjectId != null && j.IssueNum != null);

    if (!string.IsNullOrWhiteSpace(statusFilter))
        query = query.Where(j => j.IssueStatusObj.PName == statusFilter);

    query = sortBy.ToLower() switch
    {
        "created" => sortDescending ? query.OrderByDescending(j => j.Created)
                                   : query.OrderBy(j => j.Created), _ => sortDescending 
            ? query.OrderByDescending(j => j.IssueNum) 
            : query.OrderBy(j => j.IssueNum)
    };

    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(j => new
        {
            j.Id,
            j.ProjectId,
            j.IssueNum,
            j.Summary,
            j.Creator,
            j.Assignee,
            j.Created,
            StatusName = j.IssueStatusObj.PName,
            AssigneeName = j.AppUser != null && j.AppUser.User != null
                ? j.AppUser.User.DisplayName
                : "Unassigned"
        })
        .ToListAsync();

    if (!items.Any())
        return new List<BugTableDto>();

    var projectIds = items.Select(x => x.ProjectId!.Value).Distinct().ToList();
    var projectKeyDict = await _context.ProjectKeys
        .AsNoTracking()
        .Where(pk => projectIds.Contains(pk.ProjectId))
        .ToDictionaryAsync(pk => pk.ProjectId, pk => pk.ProjectKeyName);

    var creatorKeys = items.Where(x => !string.IsNullOrEmpty(x.Creator))
                           .Select(x => x.Creator!)
                           .Distinct()
                           .ToList();
    var creatorNames = await _context.AppUsers
        .AsNoTracking()
        .Where(u => creatorKeys.Contains(u.UserKey))
        .Select(u => new
        {
            u.UserKey,
            DisplayName = u.User != null ? u.User.DisplayName : u.UserKey
        })
        .ToDictionaryAsync(x => x.UserKey, x => x.DisplayName);

    var issueIds = items.Select(x => x.Id).ToList();
    var labelsDict = await _context.Labels
        .AsNoTracking()
        .Where(l => issueIds.Contains(l.IssueId))
        .GroupBy(l => l.IssueId)
        .Select(g => new
        {
            IssueId = g.Key,
            Labels = string.Join(", ", g.Select(l => l.LabelName).OrderBy(l => l))
        })
        .ToDictionaryAsync(x => x.IssueId, x => x.Labels ?? "-");

    var result = items.Select(x => new BugTableDto
    {
        Key = $"{projectKeyDict.GetValueOrDefault(x.ProjectId!.Value, "UNKNOWN")}-{x.IssueNum}",
        Summary = x.Summary ?? "Empty",
        Status = x.StatusName,
        Reporter = creatorNames.GetValueOrDefault(x.Creator, "Unknown"),
        Assignee = x.AssigneeName,
        Labels = labelsDict.GetValueOrDefault(x.Id, "-"),
        Created = x.Created,
        Age = DateTime.UtcNow - x.Created
    }).ToList();

    return result;
    }
}