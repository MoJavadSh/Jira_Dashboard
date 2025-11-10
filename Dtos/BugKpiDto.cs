namespace JiraDashboard.Dtos;

public class BugKpiDto
{
    public int TotalBugs { get; set; }
    public int OpenBugs { get; set; }
    public int ClosedBugs { get; set; }
    public double AvgTimeToCloseInDays { get; set; } 
}