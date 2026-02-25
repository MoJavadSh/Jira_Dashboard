using JiraDashboard.Dtos;

namespace JiraDashboard;

public interface IGanttChartRepository
{
    Task<List<GanttDto>> GetEpicGanttDataAsync();

    public Task<EpicDetailDto> GetEpicDetailsGanttAsync(long epicId);

}