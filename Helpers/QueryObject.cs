namespace JiraDashboard.Helpers;

public class QueryObject
{
    public string? StatusFilter { get; set; }
    public bool ExcludeUnassigned { get; set; } = false;
    public string? IssueTypeFilter { get; set; }
}