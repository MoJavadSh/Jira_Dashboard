namespace JiraDashboard.Dtos;

public class ComponentDetailDto
{
    public string ComponentName { get; set; } = string.Empty; 
    public DateTime StartDate { get; set; }
    public DateTime? DueDate { get; set; }        
    public DateTime? EndDate { get; set; }
    public int ProgressPercentage { get; set; }
    public int AssigneeCount { get; set; }
    public List<string> AssigneeNames { get; set; } = new(); 
}