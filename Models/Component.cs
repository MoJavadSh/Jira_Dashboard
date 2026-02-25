namespace JiraDashboard.Models;

public class Component
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string CName { get; set; } = string.Empty;  
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Lead { get; set; } = string.Empty;   
    public int AssigneeType { get; set; }
    public bool Archived { get; set; }
    public bool Deleted { get; set; }
    
    public Project Project { get; set; }
}