namespace JiraDashboard.Models;

public class IssueLink
{
    public long Id { get; set; }
    public long Source { get; set; }
    public long Destination { get; set; }
    public long LinkType { get; set; }
    public long Sequence { get; set; }
    public long IssueLinkField { get; set; }
}