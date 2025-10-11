using JiraDashboard.Data;
using JiraDashboard.Dtos;
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

    public async Task<List<UserBarChartDto>> GetUserBatChartAsync()
    {
        // query to get datas
        var data = from task in _context.JiraIssues
                .AsNoTracking() 
            join appUser in _context.AppUsers on task.Assignee equals appUser.UserKey into appUserJoin
            from appUser in appUserJoin.DefaultIfEmpty() 
            join user in _context.CwdUsers on appUser.LowerUserName equals user.UserName.ToLower() into userJoin
            from user in userJoin.DefaultIfEmpty()
            join issueType in _context.IssueTypes on task.IssueType equals issueType.Id
            group task by new
            {
                AssigneeName = user != null ? user.DisplayName : "Un Assigned",
                IssueTypeName = issueType.PName
            } into g
            select new
            { 
                g.Key.AssigneeName,
                g.Key.IssueTypeName,
                Count = g.Count()
            };
        var join = await data
            .GroupBy(x => x.AssigneeName)
            .Select(g => new UserBarChartDto
            {
                AssigneeName = g.Key,
                IssueTypes = g.Select(x => new IssueTypeCountDto
                {
                    IssueTypeName = x.IssueTypeName,
                    Count = x.Count
                }).ToList()
            })
            .ToListAsync();

        return join;
    }

    public async Task<List<UserIssueCountDto>> GetUserIssueCountAsync()
    {
        var data = from task in _context.JiraIssues.AsNoTracking()
            join AppUser in _context.AppUsers on task.Assignee equals AppUser.UserKey into appUserJoin
            from appUser in appUserJoin.DefaultIfEmpty()
            join user in _context.CwdUsers on appUser.Id equals user.Id into userJoin
            from user in userJoin.DefaultIfEmpty()
            group task by (user != null ? user.DisplayName : "Un Assigned") into g
            select new
            {
                AssigneeName = g.Key,
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
}

