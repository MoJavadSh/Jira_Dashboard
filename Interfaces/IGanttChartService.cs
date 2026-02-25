using JiraDashboard.Dtos;

namespace JiraDashboard;

public interface IGanttChartService
{
    Task<List<GanttDto>> GetEpicGanttDataAsync();

    public Task<EpicDetailDto> GetEpicDetailsGanttAsync(long epicId);
}