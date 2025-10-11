using JiraDashboard.Dtos;

namespace JiraDashboard;

public interface IJiraRepository
{
    Task<List<UserBarChartDto>> GetUserBatChartAsync();
    
}