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
            
            // روابط
            entity.HasOne(j => j.AppUser)
                .WithMany()
                .HasPrincipalKey(u => u.UserKey) // کلید اصلی در AppUser
                .HasForeignKey(j => j.Assignee) // کلید خارجی در JiraIssue
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
    }
}