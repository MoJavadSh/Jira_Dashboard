using JiraDashboard.Data;
using JiraDashboard.Dtos;
using Microsoft.EntityFrameworkCore;

namespace JiraDashboard.Repository;

public class JiraRepository : IJiraRepository
{
    private readonly AppDbContext _context;

    public JiraRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserBarChartDto>> GetAssigneeIssueDetailsAsync()
    {
        // query to get datas
        var rawQuery = from issue in _context.JiraIssues
            join user in _context.CwdUsers on issue.Assignee equals user.UserName into userJoin
            from user in userJoin.DefaultIfEmpty()
            join issueType in _context.IssueTypes on issue.IssueType equals issueType.Id
            group issue by new 
            { 
                AssigneeName = user != null ? user.DisplayName : "Unassigned", 
                issueType.PName 
            } into g
            select new
            {
                AssigneeName = g.Key.AssigneeName,
                IssueTypeName = g.Key.PName,
                Count = g.Count()
            };

        var grouped = await rawQuery
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

        return grouped;
    }
}

