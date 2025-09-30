namespace JiraDashboard.Models;

public class JiraIssue
{
    public long Id { get; set; }
    public string Assignee { get; set; }
    public string IssueType { get; set; }
    public string IssueStatus { get; set; }
}