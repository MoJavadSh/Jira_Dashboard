namespace JiraDashboard.Dtos;

public class UserBarChartDto
{
    public string AssigneeName { get; set; }
    public List<IssueTypeCountDto> IssueTypes { get; set; }
}