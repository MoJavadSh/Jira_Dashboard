namespace JiraDashboard.Dtos;

public class BugTableDto
{
    public string Key { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Progress { get; set; } = string.Empty;
    public string Reporter { get; set; } = string.Empty;
    public string Assignee { get; set; } = string.Empty;
    public string Labels { get; set; } = string.Empty;
    public string IssueType {get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public DateTime? DateClosed {get; set; }
    public TimeSpan LifeTime { get; set; }
}