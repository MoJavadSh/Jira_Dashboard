namespace JiraDashboard.Dtos;

public class EpicDetailDto
{
    public long EpicId { get; set; }
    public string EpicName { get; set; } = string.Empty;
    public DateTime? EpicStartDate { get; set; }
    public DateTime? EpicDueDate { get; set; }     
    public DateTime? EpicEndDate { get; set; } 
    public int EpicProgressPercentage { get; set; }
    public List<ComponentDetailDto> Components { get; set; } = new();
}