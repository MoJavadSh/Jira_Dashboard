using System.Net;
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
    /// Chart : Issue Types of each Assignee, count
    /// </summary>
    [HttpGet("UserBarChart")]
    public async Task<ActionResult<ResponseDto>> GetUserBarChart([FromQuery] bool unAssigned = true)
    {
        var result = await _repo.GetUserBarChartAsync(unAssigned);
        var response = GenerateResponse(HttpStatusCode.OK, "","Test title", "Test description", result, result.Count);
        return Ok(response);

    }

    /// <summary>
    /// Chart : Issue Count of each Assignee
    /// </summary>
    [HttpGet("UserIssueCount")]
    public async Task<ActionResult<List<UserIssueCountDto>>> GetUserIssueCountChart([FromQuery] bool unAssigned = true)
    {
        var result = await _repo.GetUserIssueCountAsync(unAssigned);
        var response = GenerateResponse(HttpStatusCode.OK, "","Test title", "Test description", result, result.Count);
        return Ok(response);
    }

    /// <summary>
    /// Chart : count of each IssueType
    /// </summary>
    [HttpGet("IssueTypeCount")]
    public async Task<ActionResult<List<IssueTypeCountDto>>> GetIssueTypeCount([FromQuery] bool unAssigned = true)
    {
        var result = await _repo.GetIssueTypeCountAsync(unAssigned);
        var response = GenerateResponse(HttpStatusCode.OK, "","Test title", "Test description", result, result.Count);
        return Ok(response);
    }

    /// <summary>
    /// Chart : Progress. Of each issue type
    /// </summary>
    [HttpGet("IssueTypeProgress")]
    public async Task<ActionResult<List<IssueTypeProgressDto>>> GetIssueTypeProgressChart([FromQuery] string? issueType,
        bool unAssigned = true)
    {
        var result = await _repo.GetIssueTypeProgressAsync(issueType, unAssigned);
        var response = GenerateResponse(HttpStatusCode.OK, "","Test title", "Test description", result, result.Count);
        return Ok(response);
    }

    /// <summary>
    /// Header : Overview of all issues (Overview Tab)
    /// </summary>
    [HttpGet("StatusSummary")]
    public async Task<ActionResult<List<OpenClosedDto>>> GetOpenClosedAsync()
    {
        var result = await _repo.GetOpenClosedAsync();
        var response = GenerateResponse(HttpStatusCode.OK, "", "Test title", "Test description", result, 1);
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

