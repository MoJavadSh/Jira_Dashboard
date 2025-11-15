namespace JiraDashboard.Models;

public class ProjectKey
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string ProjectKeyName { get; set; } 

    public Project Project { get; set; }
}