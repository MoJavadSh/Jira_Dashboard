namespace JiraDashboard.Models;

public class Label
{
    public long Id { get; set; }
    public string LabelName { get; set; } 
    public long IssueId { get; set; }     
    public long FieldId { get; set; }     
}