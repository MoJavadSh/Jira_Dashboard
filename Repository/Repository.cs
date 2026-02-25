using JiraDashboard.Data;
using JiraDashboard.Dtos;
using JiraDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace JiraDashboard.Repository;

public class Repository : IRepository
{
    private readonly AppDbContext _context;

    public Repository(AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<JiraIssue> GetIssueQuery()
        => _context.JiraIssues.AsNoTracking();

    public Task<List<JiraIssue>> GetIssuesAsync(IQueryable<JiraIssue>? query = null)
    {
        throw new NotImplementedException();
    }

    public async Task<List<ChangeItem>> GetChangeItemsAsync(
        List<long> issueIds,
        string? field = null,
        string? newValue = null,
        string? newString = null)
    {
        var q = _context.ChangeItems
            .AsNoTracking()
            .Include(ci => ci.ChangeGroup)
            .Where(ci => issueIds.Contains(ci.ChangeGroup.IssueId));

        if (field != null)
            q = q.Where(ci => ci.Field == field);

        if (newValue != null)
            q = q.Where(ci => ci.NewValue == newValue);

        if (newString != null)
            q = q.Where(ci => ci.NewString == newString);

        return await q.ToListAsync();
    }

    public async Task<List<IssueType>> GetIssueTypesAsync(string? nameContains = null)
    {
        var q = _context.IssueTypes.AsNoTracking();

        if (!string.IsNullOrEmpty(nameContains))
            q = q.Where(t => t.PName.ToLower().Contains(nameContains.ToLower()));

        return await q.ToListAsync();
    }

    public async Task<List<IssueStatus>> GetIssueStatusesAsync()
        => await _context.IssueStatuses.AsNoTracking().ToListAsync();

    public async Task<List<AppUser>> GetAppUsersAsync(List<string> userKeys)
        => await _context.AppUsers
            .AsNoTracking()
            .Include(u => u.User)
            .Where(u => userKeys.Contains(u.UserKey))
            .ToListAsync();

    public async Task<List<Label>> GetLabelsAsync(List<long> issueIds)
        => await _context.Labels
            .AsNoTracking()
            .Where(l => issueIds.Contains(l.IssueId))
            .ToListAsync();

    public async Task<List<ProjectKey>> GetProjectKeysAsync(List<long> projectIds)
        => await _context.ProjectKeys
            .AsNoTracking()
            .Where(pk => projectIds.Contains(pk.ProjectId))
            .ToListAsync();

    public async Task<List<IssueLink>> GetIssueLinksAsync(List<long> sourceIds, int linkType)
        => await _context.IssueLinks
            .AsNoTracking()
            .Where(il => il.LinkType == linkType && sourceIds.Contains(il.Source))
            .ToListAsync();

    public async Task<JiraMetadataDto> GetJiraMetadataAsync()
    {
        var progressList = await _context.IssueStatuses
            .AsNoTracking()
            .Select(s => s.PName)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        var issueTypes = await _context.IssueTypes
            .AsNoTracking()
            .Select(t => t.PName)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        var projects = await _context.ProjectKeys
            .AsNoTracking()
            .Select(pk => new ProjectDto
            {
                Key  = pk.ProjectKeyName,
                Name = pk.Project.PName
            })
            .OrderBy(p => p.Key)
            .ToListAsync();

        var assignees = await _context.JiraIssues
            .AsNoTracking()
            .Where(j => j.AppUser != null && j.AppUser.User != null)
            .Select(j => new UserDto
            {
                Name = j.AppUser.User.DisplayName,
                Key  = j.AppUser.UserKey
            })
            .Distinct()
            .OrderBy(x => x.Name)
            .ToListAsync();

        return new JiraMetadataDto
        {
            Progresses = progressList,
            IssueTypes = issueTypes,
            Projects   = projects,
            Assignees  = assignees
        };
    }
}