namespace JiraDashboard.Models;

public class ChangeGroup
{
    public long Id { get; set; }
    public long IssueId { get; set; }
    public DateTime Created { get; set; }   

    public JiraIssue JiraIssue { get; set; }
}