using JiraDashboard.Dtos;
using JiraDashboard.interfaces;
using JiraDashboard.Repository;
using Microsoft.EntityFrameworkCore;

namespace JiraDashboard.Services;

public class JiraService : IJiraService
{
    private readonly IRepository _repo;

    public JiraService(IRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<BugTableDto>> GetAllIssueAsync(
        string? assignee = null,
        string? issueType = null,
        string? progress = null,
        int? keyContains = null,
        DateTime? createdDate = null,
        DateTime? closedDate = null)
    {
        var q = _repo.Context.JiraIssues
            .AsNoTracking()
            .Include(j => j.IssueStatusObj)
            .Include(issue => issue.IssueTypeObj)
            .Include(j => j.AppUser)
            .ThenInclude(u => u.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(assignee))
            q = q.Where(j =>
                j.AppUser != null &&
                j.AppUser.User != null &&
                j.AppUser.User.DisplayName == assignee);

        if (!string.IsNullOrWhiteSpace(issueType))
            q = q.Where(j => j.IssueTypeObj.PName == issueType);

        if (!string.IsNullOrWhiteSpace(progress))
            q = q.Where(j => j.IssueStatusObj.PName == progress);

        var query = await q.Select(j => new
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
                    ? j.AppUser.User.DisplayName
                    : "Unassigned"
            })
            .ToListAsync();

        var issueIds = query.Select(x => x.Id).ToList();

        var labels = await _repo.Context.Labels
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
        var closedChanges = await _repo.Context.ChangeItems
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

        var repoterUsers = await _repo.Context.AppUsers.AsNoTracking()
            .Where(u => creatorKeys.Contains(u.UserKey))
            .Select(u => new
            {
                u.UserKey,
                DisplayName = u.User != null ? u.User.DisplayName : u.UserKey
            }).ToListAsync();

        var repoters = repoterUsers.ToDictionary(x => x.UserKey, x => x.DisplayName);

        var result = query.Select(i => new BugTableDto
        {
            Summary = i.Summary ?? "No Summary",
            Progress = i.Status,
            Assignee = i.AssigneeName,
            Reporter = repoters.GetValueOrDefault(i.Creator ?? "", "Unknown"),
            Labels = labels.GetValueOrDefault(i.Id, "-"),
            DateCreated = i.Created,
            IssueType = i.IssueTypeName,
            DateClosed = closedChanges.GetValueOrDefault(i.Id),
            LifeTime = closedChanges.GetValueOrDefault(i.Id) is DateTime closed
                ? closed - i.Created
                : DateTime.UtcNow - i.Created,
            Key = i.IssueNum,
        }).ToList();

        if (keyContains != null)
            result = result
                .Where(r => r.Key == keyContains)
                .ToList();

        if (createdDate.HasValue || closedDate.HasValue)
        {
            var createdStart = createdDate?.Date;
            var createdEnd = createdStart?.AddDays(1);
            
            var closedStart = closedDate?.Date;
            var closedEnd = closedStart?.AddDays(1);

            result = result.Where(r =>
            {
                bool createdMatch = createdDate.HasValue &&
                                    r.DateCreated >= createdStart &&
                                    r.DateCreated < createdEnd;

                bool closedMatch = closedDate.HasValue &&
                                   r.DateClosed.HasValue &&
                                   r.DateClosed.Value >= closedStart &&
                                   r.DateClosed.Value < closedEnd;

                return (createdDate.HasValue && closedDate.HasValue)
                    ? (createdMatch || closedMatch)
                    : (createdDate.HasValue ? createdMatch : closedMatch);
            }).ToList();
        }

        return result;
    }

    public async Task<JiraMetadataDto> GetJiraMetadataAsync()
    {
        var progressList = await _repo.Context.IssueStatuses
            .AsNoTracking()
            .Select(s => s.PName)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        var issueTypes = await _repo.Context.IssueTypes
            .AsNoTracking()
            .Select(t => t.PName)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        var metadata = new JiraMetadataDto
        {
            Progresses = progressList,
            IssueTypes = issueTypes,

            Projects = await _repo.Context.ProjectKeys
                .Select(pk => new ProjectDto
                {
                    Key  = pk.ProjectKeyName,
                    Name = pk.Project.PName
                })
                .OrderBy(p => p.Key)
                .ToListAsync(),

            Assignees = await _repo.Context.JiraIssues
                .Where(j => j.AppUser != null && j.AppUser.User != null)
                .Select(j => new UserDto
                {
                    Name = j.AppUser.User.DisplayName,
                    Key  = j.AppUser.UserKey
                })
                .Distinct()
                .OrderBy(x => x.Name)
                .ToListAsync()
        };

        return metadata;
    }
}