using JiraDashboard.Data;
using JiraDashboard.Dtos;
using JiraDashboard.Models;

namespace JiraDashboard.interfaces;

public interface IRepository
{
    AppDbContext Context { get; }
    IQueryable<JiraIssue> GetIssueQuery();
}