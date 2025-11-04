using System.ComponentModel.DataAnnotations.Schema;

namespace JiraDashboard.Models;

public class JiraIssue
{
    public long Id { get; set; }
    public string Assignee { get; set; }
    public string IssueType { get; set; }
    public string IssueStatus { get; set; }
    
    public AppUser AppUser { get; set; } // AppUser (L Join)
    public IssueType IssueTypeObj { get; set; } //  IssueType (In Join)
    public IssueStatus IssueStatusObj { get; set; } //  IssueStatus (In Join)
    
}