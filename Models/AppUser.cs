namespace JiraDashboard.Models;

public class AppUser
{
    public long Id { get; set; }
    public string UserKey { get; set; } 
    public string LowerUserName { get; set; } 
    public long AccountId { get; set; } 
    public long ApplicationId { get; set; } 
}