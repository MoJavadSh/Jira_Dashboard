using JiraDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace JiraDashboard.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
    {

    }
    public DbSet<JiraIssue> JiraIssues { get; set; }
    public DbSet<CwdUser> CwdUsers { get; set; }
    public DbSet<IssueType> IssueTypes { get; set; }
    public DbSet<IssueStatus> IssueStatuses { get; set; }
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<ChangeGroup> ChangeGroups { get; set; }
    public DbSet<ChangeItem> ChangeItems { get; set; }
    public DbSet<Project> Projects {get; set;}
    public DbSet<ProjectKey> ProjectKeys { get; set; }
    public DbSet<Label> Labels { get; set; }
    
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    { 
        // Map Models to Tables
        modelBuilder.HasDefaultSchema("public");
        
        modelBuilder.Entity<JiraIssue>(entity =>
        {
            entity.ToTable("jiraissue", "public"); // conncect to table jiraussye in public schema
            entity.HasKey(I => I.Id); // search with this primary key
            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("bigint");
            entity.Property(e => e.Assignee).HasColumnName("assignee").HasColumnType("varchar(255)"); 
            entity.Property(e => e.IssueType).HasColumnName("issuetype").HasColumnType("varchar(255)"); // property IssueType has column named issuetyoe in db
            entity.Property(e => e.IssueStatus).HasColumnName("issuestatus").HasColumnType("varchar(255)");
            entity.Property(e => e.Created).HasColumnName("created").HasColumnType("timestamp without time zone");
            entity.Property(e => e.Summary).HasColumnName("summary").HasColumnType("varchar(255)");
            entity.Property(e => e.ProjectId).HasColumnName("project").HasColumnType("numeric(18)");
            entity.Property(e => e.IssueNum).HasColumnName("issuenum").HasColumnType("numeric(18)");
            entity.Property(e => e.Creator).HasColumnName("creator");
            
            // Relations
            entity.HasOne(j => j.AppUser)
                .WithMany()
                .HasPrincipalKey(u => u.UserKey) // PK in AppUser
                .HasForeignKey(j => j.Assignee) // FK in JiraIssue
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.IssueTypeObj)
                .WithMany()
                .HasForeignKey(e => e.IssueType)
                .HasPrincipalKey(t => t.Id)
                .IsRequired(true) 
                .OnDelete(DeleteBehavior.NoAction);
            
            entity.HasOne(e => e.IssueStatusObj)
                .WithMany()
                .HasForeignKey(e => e.IssueStatus)
                .HasPrincipalKey(s => s.Id)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<CwdUser>(entity =>
        {
            entity.ToTable("cwd_user", "public"); 
            entity.HasKey(I => I.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("bigint");
            entity.Property(e => e.UserName).HasColumnName("user_name").HasColumnType("varchar(255)");
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasColumnType("varchar(255)");
        });

        modelBuilder.Entity<IssueType>(entity =>
        {
            entity.ToTable("issuetype", "public");
            entity.HasKey(I => I.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("varchar(255)");
            entity.Property(e => e.PName).HasColumnName("pname").HasColumnType("varchar(255)");
        });

        modelBuilder.Entity<IssueStatus>(entity =>
        {
            entity.ToTable("issuestatus", "public");
            entity.HasKey(I => I.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("varchar(255)");
            entity.Property(e => e.PName).HasColumnName("pname").HasColumnType("varchar(255)");
        });
        
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("app_user", "public");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("bigint");
            entity.Property(e => e.UserKey).HasColumnName("user_key").HasColumnType("varchar(255)");
            entity.Property(e => e.LowerUserName).HasColumnName("lower_user_name").HasColumnType("varchar(255)");
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.LowerUserName)
                .HasPrincipalKey(u => u.UserName)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        });
        
        modelBuilder.Entity<ChangeGroup>(entity =>
        {
            entity.ToTable("changegroup", "public");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IssueId).HasColumnName("issueid");
            entity.Property(e => e.Created).HasColumnName("created");

            entity.HasOne(e => e.JiraIssue)
                .WithMany()
                .HasForeignKey(e => e.IssueId)
                .HasPrincipalKey(j => j.Id);
        });

        modelBuilder.Entity<ChangeItem>(entity =>
        {
            entity.ToTable("changeitem", "public");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GroupId).HasColumnName("groupid");
            entity.Property(e => e.Field).HasColumnName("field");
            entity.Property(e => e.OldValue).HasColumnName("oldvalue");
            entity.Property(e => e.NewValue).HasColumnName("newvalue");
            entity.Property(e => e.OldString).HasColumnName("oldstring");
            entity.Property(e => e.NewString).HasColumnName("newstring");

            entity.HasOne(e => e.ChangeGroup)
                .WithMany()
                .HasForeignKey(e => e.GroupId);
        });
        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("project", "public");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("bigint");
            entity.Property(e => e.PName).HasColumnName("pname").HasColumnType("varchar(255)");
        });
        modelBuilder.Entity<ProjectKey>(entity =>
        {
            entity.ToTable("project_key", "public"); // یا گاهی "customfieldvalue" یا "projectkey"
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("bigint");
            entity.Property(e => e.ProjectId).HasColumnName("project_id").HasColumnType("bigint");
            entity.Property(e => e.ProjectKeyName).HasColumnName("project_key").HasColumnType("varchar(255)");

            entity.HasOne(e => e.Project)
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .HasPrincipalKey(p => p.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<Label>(entity =>
        {
            entity.ToTable("label", "public");
            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("numeric(18)");
            entity.Property(e => e.LabelName).HasColumnName("label").HasColumnType("varchar(255)");
            entity.Property(e => e.IssueId).HasColumnName("issue").HasColumnType("numeric(18)");
            entity.Property(e => e.FieldId).HasColumnName("fieldid").HasColumnType("numeric(18)");
        });
    }
}