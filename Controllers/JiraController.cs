using System.Net;
using JiraDashboard.Constants;
using JiraDashboard.Dtos;
using JiraDashboard.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace JiraDashboard.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JiraController : ControllerBase
{

    private readonly IJiraRepository _repo;

    public JiraController(IJiraRepository repo)
    {
        _repo = repo;
    }
   
    /// <summary>
    /// Table : all issues 
    /// </summary>
    [HttpGet("AllIssues")]
    public async Task<ActionResult<List<BugTableDto>>> GetAllIssuesAsync([FromQuery]
        string? assignee = null,
        string? issueType = null,
        string? progress = null,
        int? issueKey = null,
        DateTime? createdDate = null,
        DateTime? closedDate = null,
        int page = 1,
        int perPage = 20
        )
    {
        var result = await _repo.GetAllIssueAsync(assignee, issueType, progress, issueKey, createdDate, closedDate);
        var s = result.Skip((page - 1) * page)
            .Take(perPage).ToList();
        var response = GenerateResponse(HttpStatusCode.OK, "", "All Issues", "This table return all Issues", s, result.Count, page, perPage);
        return Ok(response);
    }
    private static ResponseDto GenerateResponse(
        HttpStatusCode statusCode,
        string message,
        string title = "",
        string description = "",
        object? data = default,
        int total = 0,
        int page = 1,
        int perPage = 10)
        => new()
        {
            StatusCode = (int)statusCode,
            Message = new List<string> { message },
            Result = new ResultDto { Title = title, Description = description, Data = data },
            Total = total,
            Page = page,
            PerPage = perPage
        };
}

