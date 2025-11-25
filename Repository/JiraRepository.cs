using System.Net;
using JiraDashboard.Data;
using JiraDashboard.Dtos;
using JiraDashboard.Helpers;
using JiraDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace JiraDashboard.Repository;

public class JiraRepository : IJiraRepository
{
    private readonly AppDbContext _context;

    public JiraRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserBarChartDto>> GetUserBarChartAsync(bool unAssigned)
    {
        var query = _context.JiraIssues.AsNoTracking()
            .Include(task => task.AppUser)
            .ThenInclude(appUser => appUser.User)
            .Include(task => task.IssueTypeObj)
            .Where(t => t.IssueTypeObj.PName != "Story");
        
        if (unAssigned)
            query = query.Where(x => x.AppUser != null && x.AppUser.User != null);
                
        var s = query
            .Select(task => new
            {
                AssigneeName = task.AppUser != null && task.AppUser.User != null
                    ? task.AppUser.User.DisplayName
                    : "Un Assigned",
                IssueTypeName = task.IssueTypeObj.PName
            })
            .GroupBy(x => new { x.AssigneeName, x.IssueTypeName })
            .Select(g => new
            {
                g.Key.AssigneeName,
                g.Key.IssueTypeName,
                count = g.Count()
            });


        var result = await s.ToListAsync();
        
        var allUsers = result.Select(x => x.AssigneeName).Distinct().ToList();
        var allIssueTypes = result.Select(x => x.IssueTypeName).Distinct().ToList();
        var completeResult = allUsers.Select(user => new UserBarChartDto
            {
                AssigneeName = user,
                IssueTypes = allIssueTypes.Select(x => new IssueTypeCountDto
                {
                    IssueTypeName = x,
                    Count = result
                        .Where(r => r.AssigneeName == user && r.IssueTypeName == x)
                        .Select(r => r.count)
                        .FirstOrDefault(), 
                    Percentage = null                
                }).ToList()
            })
            .ToList();
        return completeResult;
    }

    public async Task<List<UserIssueCountDto>> GetUserIssueCountAsync(bool unAssigned)
    {
        var tasks = _context.JiraIssues.AsNoTracking()
            .Where(task => task.IssueTypeObj.PName != "Story");
        
        if (unAssigned)
            tasks = tasks.Where(t => t.AppUser != null && t.AppUser.User != null);

        tasks = tasks
            .Include(task => task.AppUser)
            .ThenInclude(appUser => appUser.User);
            
        var task = await tasks
            .GroupBy(task =>
                task.AppUser != null && task.AppUser.User != null ? task.AppUser.User.DisplayName : "Un Assigned")
            .Select(g => new
            {
                assigneeName = g.Key,
                count = g.Count()
            }).ToListAsync();
            
        int totalIssues = task.Sum(r => r.count);
        var finalResults = task.Select(r => new UserIssueCountDto
        {
            AssigneeName = r.assigneeName,
            IssueCount = r.count,
            Percentage = totalIssues > 0 ? Math.Round((double)r.count / totalIssues * 100, 1) : 0,
        }).ToList();

        return finalResults;
    }    

    public async Task<List<IssueTypeCountDto>> GetIssueTypeCountAsync(bool unAssigned)
    {
        var tasks = _context.JiraIssues.AsNoTracking()
            .Where(task => task.IssueTypeObj.PName != "Story");
        
        if (unAssigned) 
            tasks = tasks.Where(t => t.AppUser != null && t.AppUser.User != null);

         tasks = tasks
            .Include(t => t.IssueTypeObj)
            .Include(t => t.AppUser)
            .ThenInclude(appUser => appUser.User);
         
        var task = await tasks
            .GroupBy(t => t.IssueTypeObj.PName)
            .Select(g => new
            {
                IssueTypeName = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        int totalIssues = task.Sum(r => r.Count);

        var result = task.Select(r => new IssueTypeCountDto
        {
            IssueTypeName = r.IssueTypeName,
            Count = r.Count,
            Percentage = totalIssues > 0 ? Math.Round((double)r.Count / totalIssues * 100, 1) : 0
        }).ToList();

        return result;
    }

    public async Task<List<IssueTypeProgressDto>> GetIssueTypeProgressAsync(string issueType, bool unAssigned)
    {
        var query = _context.JiraIssues.AsNoTracking()
            .Where(task => task.IssueTypeObj.PName != "Story");


        if (!string.IsNullOrWhiteSpace(issueType))
            query = query.Where(t => t.IssueTypeObj.PName == issueType); 

        if (unAssigned)
            query = query.Where(t => t.AppUser != null && t.AppUser.User != null);

        query = query
            .Include(t => t.IssueTypeObj)
            .Include(t => t.IssueStatusObj)
            .Include(t => t.AppUser).ThenInclude(a => a.User);

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

        var result = raw
            .GroupBy(x => x.IssueTypeName)
            .Select(g =>
            {
                int total = g.Sum(x => x.Count);
                return new IssueTypeProgressDto
                 {
                    IssueTypeName = g.Key,
                    Statuses = g.Select(x => new StatusCountDto
                    {
                        StatusName = x.StatusName,
                        Count = x.Count,
                        Percentage = total > 0 ? Math.Round((double)x.Count / total * 100, 1) : 0
                    }).ToList()
                };
            })
            .ToList();

        return result;
    }

    public async Task<OpenClosedDto> GetOpenClosedAsync()
    {
        var issues = await _context.JiraIssues.AsNoTracking()
            .Include(t => t.IssueStatusObj)
            .Where(task => task.IssueTypeObj.PName != "Story")
            .Select(t => new
            {
                t.Assignee,                    
                StatusName = t.IssueStatusObj.PName
            })
            .ToListAsync();

        var closedStatusNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Done", "Resolved", "Resolve"
        };

        var assignedIssues = issues.Where(t => !string.IsNullOrWhiteSpace(t.Assignee)).ToList();
        var unassignedCount = issues.Count - assignedIssues.Count;

        var openCount = assignedIssues
            .Count(t => !closedStatusNames.Contains(t.StatusName));

        var closedCount = assignedIssues
            .Count(t => closedStatusNames.Contains(t.StatusName));

        return new OpenClosedDto
        {
            Open = openCount,
            Closed = closedCount,
            Unassigned = unassignedCount
        };
    }

    public async Task<List<BugTableDto>> GetAllIssueAsync()
    {
        var query = await _context.JiraIssues
            .AsNoTracking()
            .Include(j => j.IssueStatusObj)
            .Include(issue => issue.IssueTypeObj)
            .Include(j => j.AppUser).ThenInclude(u => u.User)
            .Select(j => new
            {
                j.Id,
                j.Summary,
                Status = j.IssueStatusObj.PName,
                j.Creator,
                j.Created,
                j.ProjectId,
                j.IssueNum,
                IssueTypeName = j.IssueTypeObj.PName,
                AssigneeName = j.AppUser != null && j.AppUser.User != null
                ? j.AppUser.User.DisplayName : "Unassigned"
            })
            .ToListAsync();
        
        var issueIds = query.Select(x => x.Id).ToList();
        var labels = await _context.Labels
            .AsNoTracking()
            .Where(l => issueIds.Contains(l.IssueId))
            .GroupBy(l => l.IssueId)
            .Select(g => new
            {
                IssueId = g.Key,
                Labels = string.Join(", ", g.Select(l => l.LabelName).OrderBy(l => l))
            })
            .ToDictionaryAsync(x => x.IssueId, x => x.Labels ?? "-");


        var doneStatusId = "10001";
        var closedChanges = await _context.ChangeItems
            .AsNoTracking()
            .Include(ci => ci.ChangeGroup)
            .Where(ci => ci.Field == "status" 
                         && doneStatusId.Contains(ci.NewValue)
                         && issueIds.Contains(ci.ChangeGroup.IssueId))
            .GroupBy(ci => ci.ChangeGroup.IssueId)
            .Select(g => new
            {
                IssueId = g.Key,
                FirstClosed = g.Min(ci => ci.ChangeGroup.Created)    
            })
            .ToDictionaryAsync(x => x.IssueId, x => (DateTime?)x.FirstClosed);
        
        var creatorKeys = query
            .Where(x => !string.IsNullOrEmpty(x.Creator))
            .Select(x => x.Creator!)
            .Distinct()
            .ToList();
        var repoterUsers = await _context.AppUsers.AsNoTracking()
            .Where(u => creatorKeys.Contains(u.UserKey))
            .Select(u => new
            {
                u.UserKey,
                DisplayName = u.User != null ? u.User.DisplayName : u.UserKey
            }).ToListAsync();
        var repoters = repoterUsers.ToDictionary(x => x.UserKey, x => x.DisplayName);
        
        var projectIds = query.Where(x => x.ProjectId.HasValue)
            .Select(x => x.ProjectId!.Value)
            .Distinct()
            .ToList();
        var projectKeyDict = await _context.ProjectKeys
            .AsNoTracking()
            .Where(pk => projectIds.Contains(pk.ProjectId))
            .ToDictionaryAsync(pk => pk.ProjectId, pk => pk.ProjectKeyName);
        
        
        var result = query.Select(i => new BugTableDto()
        {
            Summary = i.Summary ?? "No Summary",
            Progress = i.Status,
            Assignee = i.AssigneeName,
            Reporter = repoters[i.Creator],
            Labels = labels.GetValueOrDefault(i.Id, "-"),
            DateCreated = i.Created,
            IssueType = i.IssueTypeName,
            DateClosed = closedChanges.GetValueOrDefault(i.Id),
            LifeTime = closedChanges.GetValueOrDefault(i.Id) is DateTime closed ? closed - i.Created : DateTime.UtcNow - i.Created,
            Key = $"{projectKeyDict.GetValueOrDefault(i.ProjectId!.Value, "UNKNOWN")}-{i.IssueNum}",
            
        }).ToList();
        return result;
    }
}


