using JiraDashboard.Dtos;
using JiraDashboard.Repository;
using Microsoft.EntityFrameworkCore;

namespace JiraDashboard.Services;

public class JiraService : IJiraService
{
    private readonly IRepository _repo;
    private const string DoneStatusId = "10001";

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
        var query = _repo.GetIssueQuery();

        if (!string.IsNullOrWhiteSpace(assignee))
            query = query.Where(j =>
                j.AppUser != null &&
                j.AppUser.User != null &&
                j.AppUser.User.DisplayName == assignee);

        if (!string.IsNullOrWhiteSpace(issueType))
            query = query.Where(j => j.IssueTypeObj.PName == issueType);

        if (!string.IsNullOrWhiteSpace(progress))
            query = query.Where(j => j.IssueStatusObj.PName == progress);

        var items = await query
            .Select(j => new
            {
                j.Id,
                j.Summary,
                Status        = j.IssueStatusObj.PName,
                j.Creator,
                j.Created,
                j.ProjectId,
                j.IssueNum,
                IssueTypeName = j.IssueTypeObj.PName,
                AssigneeName  = j.AppUser != null && j.AppUser.User != null
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

        var closedChangesTask = _repo.GetChangeItemsAsync(issueIds, field: "status", newValue: DoneStatusId);
        var creatorUsersTask  = _repo.GetAppUsersAsync(creatorKeys);
        var labelsTask        = _repo.GetLabelsAsync(issueIds);
        var projectIds        = items.Where(x => x.ProjectId.HasValue).Select(x => x.ProjectId!.Value).Distinct().ToList();
        var projectKeysTask   = _repo.GetProjectKeysAsync(projectIds);

        await Task.WhenAll(closedChangesTask, creatorUsersTask, labelsTask, projectKeysTask);

        var closedChanges = closedChangesTask.Result
            .GroupBy(ci => ci.ChangeGroup.IssueId)
            .ToDictionary(
                g => g.Key,
                g => (DateTime?)g.Min(ci => ci.ChangeGroup.Created));

        var reporters = creatorUsersTask.Result
            .ToDictionary(
                u => u.UserKey,
                u => u.User != null ? u.User.DisplayName : u.UserKey);

        var labelsDict = labelsTask.Result
            .GroupBy(l => l.IssueId)
            .ToDictionary(
                g => g.Key,
                g => string.Join(", ", g.Select(l => l.LabelName).OrderBy(l => l)));

        var result = items.Select(i => new BugTableDto
        {
            Key         = i.IssueNum,
            Summary     = i.Summary ?? "No Summary",
            Progress    = i.Status,
            Assignee    = i.AssigneeName,
            Reporter    = reporters.GetValueOrDefault(i.Creator ?? "", "Unknown"),
            Labels      = labelsDict.GetValueOrDefault(i.Id, "-"),
            IssueType   = i.IssueTypeName,
            DateCreated = i.Created,
            DateClosed  = closedChanges.GetValueOrDefault(i.Id),
            LifeTime    = closedChanges.GetValueOrDefault(i.Id) is DateTime closed
                ? closed - i.Created
                : DateTime.UtcNow - i.Created,
        }).ToList();

        if (keyContains != null)
            result = result.Where(r => r.Key == keyContains).ToList();

        if (createdDate.HasValue || closedDate.HasValue)
        {
            var createdStart = createdDate?.Date;
            var createdEnd   = createdStart?.AddDays(1);
            var closedStart  = closedDate?.Date;
            var closedEnd    = closedStart?.AddDays(1);

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
        return await _repo.GetJiraMetadataAsync();

    }
        
}