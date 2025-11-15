namespace JiraDashboard.Dtos;

public class BugTableDto
{
    public string Key { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Reporter { get; set; } = string.Empty;
    public string Assignee { get; set; } = string.Empty;
    public string Labels { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public TimeSpan Age { get; set; }
}