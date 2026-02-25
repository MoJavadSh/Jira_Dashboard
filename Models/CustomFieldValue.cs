using System.ComponentModel.DataAnnotations.Schema;

namespace JiraDashboard.Models;

[Table("customfieldvalue", Schema = "public")]
public class CustomFieldValue
{
    [Column("id")]
    public long Id { get; set; }

    [Column("issue")]
    public long IssueId { get; set; } // FK to jiraissue.id

    [Column("customfield")]
    public long CustomFieldId { get; set; } // FK to customfield.id

    [Column("datevalue")]
    public DateTime? DateValue { get; set; }
}