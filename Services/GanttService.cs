using JiraDashboard.Dtos;
using JiraDashboard.Repository;
using Microsoft.EntityFrameworkCore;

namespace JiraDashboard.Services;
public class GanttService : IGanttService
{
    private readonly IRepository _repo;
    private static readonly Random _random = new Random();

    private const long FrontEndId = 10001;
    private const long BackEndId  = 10002;
    private const long QAId       = 10003;

    private const int EpicToStoryLinkType    = 10201;
    private const int StoryToSubTaskLinkType = 10200;

    private static readonly string[] DoneStatuses = { "Done", "Resolved" };

    public GanttService(IRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<GanttDto>> GetEpicGanttDataAsync()
    {
        var epicTypes  = await _repo.GetIssueTypesAsync("epic");
        var epicTypeId = epicTypes.FirstOrDefault()?.Id;

        if (string.IsNullOrEmpty(epicTypeId)) return new List<GanttDto>();

        var epics = await _repo.GetIssueQuery()
            .Where(j => j.IssueType == epicTypeId)
            .Select(j => new
            {
                j.Id,
                j.Summary,
                j.Created,
                j.DueDate,
                j.ResolutionDate,
                Status = j.IssueStatusObj != null ? j.IssueStatusObj.PName : "Unknown"
            })
            .ToListAsync();

        if (!epics.Any()) return new List<GanttDto>();

        var epicIds        = epics.Select(e => e.Id).ToList();
        var epicStoryLinks = await _repo.GetIssueLinksAsync(epicIds, EpicToStoryLinkType);

        var allStoryIds = epicStoryLinks.Select(l => l.Destination).Distinct().ToList();
        var storyToEpic = epicStoryLinks
            .GroupBy(l => l.Destination)
            .ToDictionary(g => g.Key, g => g.First().Source);

        List<long>            allSubTaskIds  = new();
        Dictionary<long,long> subTaskToStory = new();

        if (allStoryIds.Any())
        {
            var storySubLinks = await _repo.GetIssueLinksAsync(allStoryIds, StoryToSubTaskLinkType);
            allSubTaskIds  = storySubLinks.Select(l => l.Destination).Distinct().ToList();
            subTaskToStory = storySubLinks
                .GroupBy(l => l.Destination)
                .ToDictionary(g => g.Key, g => g.First().Source);
        }

        var subTaskStatuses = allSubTaskIds.Any()
            ? await _repo.GetIssueQuery()
                .Where(t => allSubTaskIds.Contains(t.Id))
                .Select(t => new
                {
                    t.Id,
                    StatusName = t.IssueStatusObj != null ? t.IssueStatusObj.PName : ""
                })
                .ToListAsync()
            : new();

        var epicSubStatuses = new Dictionary<long, List<string>>();
        foreach (var sub in subTaskStatuses)
        {
            if (!subTaskToStory.TryGetValue(sub.Id, out var storyId)) continue;
            if (!storyToEpic.TryGetValue(storyId, out var epicId))    continue;

            if (!epicSubStatuses.ContainsKey(epicId))
                epicSubStatuses[epicId] = new List<string>();

            epicSubStatuses[epicId].Add(sub.StatusName);
        }

        return epics.Select(e =>
        {
            var statuses = epicSubStatuses.GetValueOrDefault(e.Id, new List<string>());
            var total    = statuses.Count;
            var done     = statuses.Count(s => DoneStatuses.Contains(s));

            return new GanttDto
            {
                Id         = e.Id,
                Name       = e.Summary ?? "Epic بدون نام",
                StartDate  = e.Created,
                DueDate    = e.DueDate,
                EndDate    = e.ResolutionDate,
                Status     = e.Status,
                Percentage = _random.Next(30, 90)
            };
        }).ToList();
    }

    public async Task<EpicDetailDto> GetEpicDetailsGanttAsync(long epicId)
    {
        var epic = await _repo.GetIssueQuery()
            .Where(j => j.Id == epicId
                     && j.IssueTypeObj != null
                     && j.IssueTypeObj.PName.ToLower() == "epic")
            .Select(j => new
            {
                j.Id,
                j.Summary,
                j.Created,
                j.DueDate,
                j.ResolutionDate
            })
            .FirstOrDefaultAsync();

        if (epic == null)
            return EmptyDetail(epicId, "Epic پیدا نشد");

        var epicLinks = await _repo.GetIssueLinksAsync(new List<long> { epicId }, EpicToStoryLinkType);
        var storyIds  = epicLinks.Select(l => l.Destination).ToList();

        if (!storyIds.Any())
            return EmptyDetail(epicId, epic.Summary, epic.Created, epic.DueDate, epic.ResolutionDate);

        var storySubLinks = await _repo.GetIssueLinksAsync(storyIds, StoryToSubTaskLinkType);
        var subTaskIds    = storySubLinks.Select(l => l.Destination).ToList();

        if (!subTaskIds.Any())
            return EmptyDetail(epicId, epic.Summary, epic.Created, epic.DueDate, epic.ResolutionDate);

        var tasks = await _repo.GetIssueQuery()
            .Where(t => subTaskIds.Contains(t.Id))
            .Select(t => new
            {
                t.Component,
                t.Created,
                t.DueDate,
                t.ResolutionDate,
                StatusName = t.IssueStatusObj != null ? t.IssueStatusObj.PName : ""
            })
            .ToListAsync();

        var componentDefs = new[]
        {
            new { Id = FrontEndId, Name = "Front-end" },
            new { Id = BackEndId,  Name = "Back-end"  },
            new { Id = QAId,       Name = "QA"        }
        };

        var components = new List<ComponentDetailDto>();

        foreach (var def in componentDefs)
        {
            var ct = tasks.Where(t => t.Component == def.Id).ToList();
            if (!ct.Any()) continue;

            var total = ct.Count;
            var done  = ct.Count(t => DoneStatuses.Contains(t.StatusName));

            components.Add(new ComponentDetailDto
            {
                ComponentName      = def.Name,
                StartDate          = ct.Min(t => t.Created),
                DueDate            = ct.Any(t => t.DueDate.HasValue)
                                         ? ct.Where(t => t.DueDate.HasValue).Max(t => t.DueDate)
                                         : null,
                EndDate            = ct.Any(t => t.ResolutionDate.HasValue)
                                         ? ct.Where(t => t.ResolutionDate.HasValue).Max(t => t.ResolutionDate)
                                         : null,
                ProgressPercentage = total > 0 ? (done * 100) / total : 0,
                AssigneeCount      = 0
            });
        }

        var totalAll = tasks.Count;
        var doneAll  = tasks.Count(t => DoneStatuses.Contains(t.StatusName));

        return new EpicDetailDto
        {
            EpicId                 = epicId,
            EpicName               = epic.Summary ?? "Epic بدون نام",
            EpicStartDate          = tasks.Any() ? tasks.Min(t => t.Created) : epic.Created,
            EpicDueDate            = epic.DueDate,
            EpicEndDate            = epic.ResolutionDate,
            EpicProgressPercentage = totalAll > 0 ? (doneAll * 100) / totalAll : 0,
            Components             = components
        };
    }

    private static EpicDetailDto EmptyDetail(
        long      epicId,
        string?   name,
        DateTime? start = null,
        DateTime? due   = null,
        DateTime? end   = null) => new()
    {
        EpicId                 = epicId,
        EpicName               = name ?? "Epic بدون نام",
        EpicStartDate          = start,
        EpicDueDate            = due,
        EpicEndDate            = end,
        EpicProgressPercentage = 0,
        Components             = new List<ComponentDetailDto>()
    };
}