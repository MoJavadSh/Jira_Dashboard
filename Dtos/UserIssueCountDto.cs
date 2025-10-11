namespace JiraDashboard.Dtos;

public class UserIssueCountDto
{
    public string AssigneeName { get; set; }
    public int IssueCount { get; set; }
    public double Percentage { get; set; }
}