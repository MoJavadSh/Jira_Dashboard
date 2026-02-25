using JiraDashboard.Data;
using JiraDashboard.Dtos;
using JiraDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace JiraDashboard.Repository
{
    public class GanttChartRepository : IGanttChartRepository
    {
        private static readonly Random _random = new Random();
        private readonly AppDbContext _context;

        // Fixed Component IDs as per spec
        private const long FrontEndId = 10001;
        private const long BackEndId  = 10002;
        private const long QAId       = 10003;

        private static readonly string[] DoneStatuses = { "Done", "Resolved" };

        public GanttChartRepository(AppDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────────────
        //  API 1  –  GET /api/Gantt/EpicGantt
        // ─────────────────────────────────────────────────────────
        public async Task<List<GanttDto>> GetEpicGanttDataAsync()
        {
            // 1. Resolve "Epic" issue-type ID
            var epicTypeId = await _context.IssueTypes
                .AsNoTracking()
                .Where(t => t.PName.ToLower() == "epic")
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(epicTypeId))
                return new List<GanttDto>();

            // 2. Fetch Epics – project only needed columns to avoid phantom FK columns
            var epics = await _context.JiraIssues
                .AsNoTracking()
                .Where(j => j.IssueType == epicTypeId)
                .Select(j => new
                {
                    j.Id,
                    j.Summary,
                    j.Created,
                    j.DueDate,
                    j.ResolutionDate,
                    // Inline join to issuestatus – no Include() needed
                    Status = j.IssueStatusObj != null ? j.IssueStatusObj.PName : "Unknown"
                })
                .ToListAsync();

            if (!epics.Any())
                return new List<GanttDto>();

            var epicIds = epics.Select(e => e.Id).ToList();

            // 3. Epic → Story  (linktype = 10201)
            var epicStoryLinks = await _context.IssueLinks
                .AsNoTracking()
                .Where(il => il.LinkType == 10201 && epicIds.Contains(il.Source))
                .Select(il => new { il.Source, il.Destination })
                .ToListAsync();

            var allStoryIds = epicStoryLinks.Select(l => l.Destination).Distinct().ToList();

            // Lookup: storyId → epicId
            var storyToEpic = epicStoryLinks
                .GroupBy(l => l.Destination)
                .ToDictionary(g => g.Key, g => g.First().Source);

            List<long>            allSubTaskIds  = new();
            Dictionary<long,long> subTaskToStory = new();

            if (allStoryIds.Any())
            {
                // 4. Story → Sub-task  (linktype = 10200)
                var storySubLinks = await _context.IssueLinks
                    .AsNoTracking()
                    .Where(il => il.LinkType == 10200 && allStoryIds.Contains(il.Source))
                    .Select(il => new { il.Source, il.Destination })
                    .ToListAsync();

                allSubTaskIds = storySubLinks.Select(l => l.Destination).Distinct().ToList();
                subTaskToStory = storySubLinks
                    .GroupBy(l => l.Destination)
                    .ToDictionary(g => g.Key, g => g.First().Source);
            }

            // 5. Fetch only status name for sub-tasks (no Include, no phantom columns)
            var subTaskStatuses = allSubTaskIds.Any()
                ? await _context.JiraIssues
                    .AsNoTracking()
                    .Where(t => allSubTaskIds.Contains(t.Id))
                    .Select(t => new
                    {
                        t.Id,
                        StatusName = t.IssueStatusObj != null ? t.IssueStatusObj.PName : ""
                    })
                    .ToListAsync()
                : new();

            // 6. epicId → subtask status list
            var epicSubStatuses = new Dictionary<long, List<string>>();
            foreach (var sub in subTaskStatuses)
            {
                if (!subTaskToStory.TryGetValue(sub.Id, out var storyId))   continue;
                if (!storyToEpic   .TryGetValue(storyId, out var epicId))   continue;

                if (!epicSubStatuses.ContainsKey(epicId))
                    epicSubStatuses[epicId] = new List<string>();

                epicSubStatuses[epicId].Add(sub.StatusName);
            }

            // 7. Build result
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
                    // Percentage = total > 0 ? (done * 100) / total : 0
                    Percentage = _random.Next(30, 90)

                };
            }).ToList();
        }

        // ─────────────────────────────────────────────────────────
        //  API 2  –  GET /api/Gantt/EpicDetail?epicId={id}
        // ─────────────────────────────────────────────────────────
        public async Task<EpicDetailDto> GetEpicDetailsGanttAsync(long epicId)
        {
            // 1. Fetch the Epic row using Select (avoid Include + phantom FK issue)
            var epic = await _context.JiraIssues
                .AsNoTracking()
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

            // 2. Epic → Stories  (linktype = 10201)
            var storyIds = await _context.IssueLinks
                .AsNoTracking()
                .Where(il => il.Source == epicId && il.LinkType == 10201)
                .Select(il => il.Destination)
                .ToListAsync();

            if (!storyIds.Any())
                return EmptyDetail(epicId, epic.Summary, epic.Created, epic.DueDate, epic.ResolutionDate);

            // 3. Stories → Sub-tasks  (linktype = 10200)
            var subTaskIds = await _context.IssueLinks
                .AsNoTracking()
                .Where(il => storyIds.Contains(il.Source) && il.LinkType == 10200)
                .Select(il => il.Destination)
                .ToListAsync();

            if (!subTaskIds.Any())
                return EmptyDetail(epicId, epic.Summary, epic.Created, epic.DueDate, epic.ResolutionDate);

            // 4. Fetch sub-task details using Select only (no Include)
            var tasks = await _context.JiraIssues
                .AsNoTracking()
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

            // 5. Per-component breakdown
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

            // 6. Overall epic progress
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

        // ─── Helpers ───────────────────────────────────────────────
        private static EpicDetailDto EmptyDetail(
            long     epicId,
            string?  name,
            DateTime?  start = null,
            DateTime?  due   = null,
            DateTime?  end   = null) => new()
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
}
