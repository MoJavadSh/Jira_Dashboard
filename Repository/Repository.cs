using JiraDashboard.Data;
using JiraDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace JiraDashboard.Repository;


public class Repository : IRepository
{
    private readonly AppDbContext _context;

    public Repository(AppDbContext context) => _context = context;

    public IQueryable<JiraIssue> GetIssueQuery() =>
        _context.JiraIssues.AsNoTracking();

    public async Task<List<JiraIssue>> GetIssuesAsync(IQueryable<JiraIssue>? query = null) =>
        await (query ?? _context.JiraIssues.AsNoTracking()).ToListAsync();

    public async Task<List<ChangeItem>> GetChangeItemsAsync(
        List<long> issueIds, string? field = null, string? newValue = null)
    {
        var q = _context.ChangeItems.AsNoTracking()
            .Include(ci => ci.ChangeGroup)
            .Where(ci => issueIds.Contains(ci.ChangeGroup.IssueId));

        if (field != null) q = q.Where(ci => ci.Field == field);
        if (newValue != null) q = q.Where(ci => ci.NewValue == newValue);

        return await q.ToListAsync();
    }

    public async Task<List<IssueType>> GetIssueTypesAsync(string? nameFilter = null)
    {
        var q = _context.IssueTypes.AsNoTracking();
        if (nameFilter != null)
            q = q.Where(t => t.PName.ToLower().Contains(nameFilter.ToLower()));
        return await q.ToListAsync();
    }

    public async Task<List<IssueStatus>> GetIssueStatusesAsync() =>
        await _context.IssueStatuses.AsNoTracking().ToListAsync();

    public async Task<List<AppUser>> GetAppUsersAsync(List<string> userKeys) =>
        await _context.AppUsers.AsNoTracking()
            .Where(u => userKeys.Contains(u.UserKey))
            .Include(u => u.User)
            .ToListAsync();

    public async Task<List<Label>> GetLabelsAsync(List<long> issueIds) =>
        await _context.Labels.AsNoTracking()
            .Where(l => issueIds.Contains(l.IssueId))
            .ToListAsync();

    public async Task<List<ProjectKey>> GetProjectKeysAsync(List<long> projectIds) =>
        await _context.ProjectKeys.AsNoTracking()
            .Where(pk => projectIds.Contains(pk.ProjectId))
            .ToListAsync();

    public async Task<List<IssueLink>> GetIssueLinksAsync(List<long> sourceIds, int linkType) =>
        await _context.IssueLinks.AsNoTracking()
            .Where(il => il.LinkType == linkType && sourceIds.Contains(il.Source))
            .ToListAsync();
}
