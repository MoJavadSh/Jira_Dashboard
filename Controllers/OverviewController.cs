using System.Net;
using JiraDashboard.Constants;
using JiraDashboard.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace JiraDashboard.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OverviewController : ControllerBase
{
    private readonly IOverviewRepository _repository;

    public OverviewController(IOverviewRepository repository)
    {
        _repository = repository;
    }
    
    
    /// <summary>
    /// Chart : Issue Types of each Assignee, count
    /// </summary>
    [HttpGet("UserBarChart")]
    public async Task<ActionResult<ResponseDto>> GetUserBarChart([FromQuery] bool unAssigned = true)
    {
        var result = await _repository.GetUserBarChartAsync(unAssigned);
        var response = GenerateResponse(HttpStatusCode.OK, "",JiraText.UserBarChart.Title, JiraText.UserBarChart.Description, result, result.Count);
        return Ok(response);

    }

    /// <summary>
    /// Chart : Issue Count of each Assignee
    /// </summary>
    [HttpGet("UserIssueCount")]
    public async Task<ActionResult<List<UserIssueCountDto>>> GetUserIssueCountChart([FromQuery] bool unAssigned = true)
    {
        var result = await _repository.GetUserIssueCountAsync(unAssigned);
        var response = GenerateResponse(HttpStatusCode.OK, "",JiraText.UserIssueCount.Title, JiraText.UserIssueCount.Description, result, result.Count);
        return Ok(response);
    }

    /// <summary>
    /// Chart : count of each IssueType
    /// </summary>
    [HttpGet("IssueTypeCount")]
    public async Task<ActionResult<List<IssueTypeCountDto>>> GetIssueTypeCount([FromQuery] bool unAssigned = true)
    {
        var result = await _repository.GetIssueTypeCountAsync(unAssigned);
        var response = GenerateResponse(HttpStatusCode.OK, "",JiraText.IssueTypeCount.Title, JiraText.IssueTypeCount.Description, result, result.Count);
        return Ok(response);
    }

    /// <summary>
    /// Chart : Progress. Of each issue type
    /// </summary>
    [HttpGet("IssueTypeProgress")]
    public async Task<ActionResult<List<IssueTypeProgressDto>>> GetIssueTypeProgressChart([FromQuery] string? issueType,
        bool unAssigned = true)
    {
        var result = await _repository.GetIssueTypeProgressAsync(issueType, unAssigned);
        var response = GenerateResponse(HttpStatusCode.OK, "",JiraText.IssueTypeProgress.Title, JiraText.IssueTypeProgress.Description, result, result.Count);
        return Ok(response);
    }

    /// <summary>
    /// Header : Overview of all issues (Overview Tab)
    /// </summary>
    [HttpGet("StatusSummary")]
    public async Task<ActionResult<List<OpenClosedDto>>> GetOpenClosedAsync()
    {
        var result = await _repository.GetOpenClosedAsync();
        var response = GenerateResponse(HttpStatusCode.OK, "", JiraText.StatusSummary.Title, JiraText.StatusSummary.Description, result, 1);
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