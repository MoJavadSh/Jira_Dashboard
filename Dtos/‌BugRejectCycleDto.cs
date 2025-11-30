namespace JiraDashboard.Dtos;

public class BugRejectCycleDto
{
    // public string IssueKey { get; set; } = string.Empty;
    public long IssueKey { get; set; }
    public int ReopenCount { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Assignee { get; set; } = "Unassigned";
}