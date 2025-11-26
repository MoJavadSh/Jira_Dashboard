namespace JiraDashboard.Dtos;

public class JiraMetadataDto
{
    public List<string>? Progresses {get; set;}
    public List<ProjectDto>? Projects {get; set;} 
    public List<string>? IssueTypes {get; set;} 
    public List<UserDto>? Assignees {get; set;} 
    
}
public class UserDto
{
    public string Key { get; set; } = string.Empty;    
    public string Name { get; set; } = string.Empty;   
}

public class ProjectDto
{
    public string Key { get; set; } = string.Empty;    
    public string Name { get; set; } = string.Empty;
}