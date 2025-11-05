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
        var s = _context.JiraIssues.AsNoTracking()
            .Include(task => task.AppUser)
            .ThenInclude(appUser => appUser.User)
            .Include(task => task.IssueTypeObj)
            .Select(task => new
            {
                AssigneeName = task.AppUser != null && task.AppUser.User != null
                    ? task.AppUser.User.DisplayName
                    : "Un Assigned",
                IssueTypeName = task.IssueTypeObj.PName
            })
            .Where(x=>unAssigned == true ? x.AssigneeName != "Un Assigned":true)
            .GroupBy(x => new { x.AssigneeName, x.IssueTypeName })
            .Select(g => new
            {
                g.Key.AssigneeName,
                g.Key.IssueTypeName,
                count = g.Count()
            });


        var result = await s.ToListAsync();
        
        var group = result
            .GroupBy(x => x.AssigneeName)
            .Select(g => new UserBarChartDto
            {
                AssigneeName = g.Key,
                IssueTypes = g.Select(x => new IssueTypeCountDto
                {
                    IssueTypeName = x.IssueTypeName,
                    Count = x.count,
                    Percentage = null
                }).ToList()
            })
            .ToList();
        return group;
    }

    public async Task<List<UserIssueCountDto>> GetUserIssueCountAsync(bool unAssigned)
    {
        var tasks = _context.JiraIssues.AsNoTracking();
        
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
        var tasks = _context.JiraIssues.AsNoTracking();
        
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
        var query = _context.JiraIssues.AsNoTracking();

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
}


