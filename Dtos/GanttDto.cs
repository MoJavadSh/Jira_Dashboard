namespace JiraDashboard.Dtos;

public class GanttDto
{
    public long Id { get; set; } 
    public string Name { get; set; }        
    public DateTime StartDate { get; set; }
    public DateTime? DueDate { get; set; }    
    public DateTime? EndDate { get; set; }    
    public string Status { get; set; }   
    public string ProjectName { get; set; }   

    public int Percentage{ get;set;}
}
