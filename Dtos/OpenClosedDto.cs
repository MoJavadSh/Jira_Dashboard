namespace JiraDashboard.Dtos;

public class OpenClosedDto
{
    public int Open { get; set; }
    public int Closed { get; set; }
    public int Unassigned { get; set; }

    public int Total => Open + Closed + Unassigned;
    public int AssignedTotal => Open + Closed;
    public double ClosedPercentage => AssignedTotal > 0 ? Math.Round(100.0 * Closed / AssignedTotal, 1) : 0;
    public double UnassignedPercentage => Total > 0 ? Math.Round(100.0 * Unassigned / Total, 1) : 0;
}