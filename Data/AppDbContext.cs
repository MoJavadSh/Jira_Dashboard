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
    
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    { 
        // Map Models to Tables
        modelBuilder.HasDefaultSchema("public");
        
        modelBuilder.Entity<JiraIssue>(entity =>
        {
            entity.ToTable("jiraissue", "public"); // conncect to table jiraussye in public schema
            entity.HasKey(I => I.Id); // search with this primary key
            entity.Property(e => e.Assignee).HasColumnType("varchar(255)"); 
            entity.Property(e => e.IssueType).HasColumnName("issuetype").HasColumnType("varchar(255)"); // property IssueType has column named issuetyoe in db
            entity.Property(e => e.IssueStatus).HasColumnName("issuestatus").HasColumnType("varchar(255)");
        });

        modelBuilder.Entity<CwdUser>(entity =>
        {
            entity.ToTable("cwd_user", "public"); 
            entity.HasKey(I => I.Id);
            entity.Property(e => e.UserName).HasColumnName("user_name").HasColumnType("varchar(255)");
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasColumnType("varchar(255)");
        });

        modelBuilder.Entity<IssueType>(entity =>
        {
            entity.ToTable("issuetype", "public");
            entity.HasKey(I => I.Id);
            entity.Property(e => e.Id).HasColumnType("varchar(255)");
            entity.Property(e => e.PName).HasColumnName("p_name").HasColumnType("varchar(255)");
        });

        modelBuilder.Entity<IssueStatus>(entity =>
        {
            entity.ToTable("issuestatus", "public");
            entity.HasKey(I => I.Id);
            entity.Property(e => e.Id).HasColumnType("varchar(255)");
            entity.Property(e => e.PName).HasColumnName("p_name").HasColumnType("varchar(255)");
        });
    }
}