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

    public async Task<List<UserBarChartDto>> GetUserBatChartAsync(bool unAssign)
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
            .Where(x=>unAssign == true ? x.AssigneeName != "Un Assigned":true)
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

    public async Task<List<UserIssueCountDto>> GetUserIssueCountAsync(QueryObject query)
    {
        var data = from task in _context.JiraIssues.AsNoTracking()
            join AppUser in _context.AppUsers on task.Assignee equals AppUser.UserKey into appUserJoin
            from appUser in appUserJoin.DefaultIfEmpty()
            join user in _context.CwdUsers on appUser.Id equals user.Id into userJoin
            from user in userJoin.DefaultIfEmpty()
            join issueType in _context.IssueTypes on task.IssueType equals issueType.Id
            where !query.ExcludeUnassigned || user != null
            group task by new {AssigneeName = user != null ? user.DisplayName : "Un Assigned"} into g
            select new
            {
                AssigneeName = g.Key.AssigneeName,
                Count = g.Count()
            };
        
        var results = await data.ToListAsync();
        
        int totalIssues = results.Sum(r => r.Count);
        
        var finalResults = results.Select(r => new UserIssueCountDto
        {
            AssigneeName = r.AssigneeName,
            IssueCount = r.Count,
            Percentage = totalIssues > 0 ? Math.Round((double)r.Count / totalIssues * 100, 1) : 0
            
        }).ToList();

        return finalResults;
            
    }

    public async Task<List<IssueTypeCountDto>> GetIssueTypeCountAsync(QueryObject query)
    {
        var data = from task in _context.JiraIssues.AsNoTracking()
            join issueType in _context.IssueTypes on task.IssueType equals issueType.Id
            join appUser in _context.AppUsers on task.Assignee equals appUser.UserKey into appUserJoin
            from appUser in appUserJoin.DefaultIfEmpty()
            join user in _context.CwdUsers on appUser.LowerUserName equals user.UserName.ToLower() into userJoin
            from user in userJoin.DefaultIfEmpty()
            where !query.ExcludeUnassigned || user != null // filter
            group task by issueType.PName into g
            select new
            {
                IssueTypeName = g.Key,
                Count = g.Count()
            };   
        var results = await data.ToListAsync();
        
        int totalIssues = results.Sum(r => r.Count);
        
        var finalResults = results.Select(r => new IssueTypeCountDto
        {
            IssueTypeName = r.IssueTypeName,
            Count = r.Count,
            Percentage = totalIssues > 0 ? Math.Round((double)r.Count / totalIssues * 100, 1) : 0
        }).ToList();

        return finalResults;
    }

    public async Task<List<IssueTypeProgressDto>> GetIssueTypeProgressAsync(QueryObject query)
    {
        var rawQuery = from task in _context.JiraIssues.AsNoTracking()
                       join issueType in _context.IssueTypes on task.IssueType equals issueType.Id
                       join status in _context.IssueStatuses on task.IssueStatus equals status.Id
                       join appUser in _context.AppUsers on task.Assignee equals appUser.UserKey into appUserJoin
                       from appUser in appUserJoin.DefaultIfEmpty()
                       join user in _context.CwdUsers on appUser.LowerUserName equals user.UserName.ToLower() into userJoin
                       from user in userJoin.DefaultIfEmpty()
                        where (string.IsNullOrEmpty(query.IssueTypeFilter) || task.IssueType == query.IssueTypeFilter) 
                           && (!query.ExcludeUnassigned || user != null) 
                           && (string.IsNullOrEmpty(query.StatusFilter) || task.IssueStatus == query.StatusFilter)
                       group task by new { IssueTypeName = issueType.PName, StatusName = status.PName } into g
                       select new
                       {
                           g.Key.IssueTypeName,
                           g.Key.StatusName,
                           Count = g.Count()
                       };

        var results = await rawQuery.ToListAsync();

        var grouped = results
            .GroupBy(x => x.IssueTypeName)
            .Select(g => new IssueTypeProgressDto
            {
                IssueTypeName = g.Key,
                Statuses = g.Select(x => new StatusCountDto
                {
                    StatusName = x.StatusName,
                    Count = x.Count
                }).ToList()
            })
            .ToList();

        return grouped;
    }}

