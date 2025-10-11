namespace JiraDashboard.Dtos;

public class IssueTypeProgressDto
{
    public string IssueTypeName { get; set; } 
    public List<StatusCountDto> Statuses { get; set; } 
}