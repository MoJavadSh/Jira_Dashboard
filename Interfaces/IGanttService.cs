using JiraDashboard.Dtos;

namespace JiraDashboard;

public interface IGanttService
{
    Task<List<GanttDto>> GetEpicGanttDataAsync();

    
    public Task<EpicDetailDto> GetEpicDetailsGanttAsync(long epicId);
}