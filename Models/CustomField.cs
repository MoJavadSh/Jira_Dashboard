using System.ComponentModel.DataAnnotations.Schema;

namespace JiraDashboard.Models;

[Table("customfield", Schema = "public")]
public class CustomField
{
        [Column("id")]
        public string Id { get; set; } 

        [Column("customfieldtypekey")]
        public string CustomFieldTypeKey { get; set; }

        [Column("cfname")]
        public string FieldName { get; set; }
}