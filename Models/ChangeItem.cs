namespace JiraDashboard.Models;

public class ChangeItem
{
    public long Id { get; set; }
    public long GroupId { get; set; }
    public string Field { get; set; }      
    public string OldValue { get; set; }  
    public string OldString { get; set; }
    public string NewValue { get; set; } 
    public string NewString { get; set; }

    public ChangeGroup ChangeGroup { get; set; }
}