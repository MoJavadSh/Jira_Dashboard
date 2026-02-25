using JiraDashboard.Data;
using JiraDashboard.interfaces;
using JiraDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace JiraDashboard.Repository;

public class Repository : IRepository
{
    public AppDbContext Context { get; }
    
    public Repository(AppDbContext context)
    {
        Context = context;
    }
    
    public IQueryable<JiraIssue> GetIssueQuery()
        => Context.JiraIssues.AsNoTracking();
}