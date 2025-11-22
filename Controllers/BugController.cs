using System.Net;
using JiraDashboard.Constants;
using JiraDashboard.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace JiraDashboard.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BugController : ControllerBase
{
    private readonly IBugRepository _repo;

    public BugController(IBugRepository repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// Chart : closed vs open everyDay
    /// </summary>
    [HttpGet("BugDaily")]
    public async Task<ActionResult<ResponseDto>> GetBugDailyTrendAsync([FromQuery] bool unAssigned = true)
    {
        var result = await _repo.GetBugDailyTrendAsync();
        var response = GenerateResponse(HttpStatusCode.OK, "", BugText.BugDaily.Title, BugText.BugDaily.Description,
            result, result.Count);
        return Ok(response);
    }

    /// <summary>
    /// Header : Status of all bugs
    /// </summary>
    [HttpGet("BugStatus")]
    public async Task<ActionResult<ResponseDto>> GetBugStatus()
    {
        var result = await _repo.GetBugStatus();
        var response = GenerateResponse(HttpStatusCode.OK, "", BugText.BugStatus.Title, BugText.BugStatus.Description,
            result, 1);
        return Ok(response);
    }

    /// <summary>
    /// Chart(horizontal Bar) : Cycle of rejected Bugs (Hover datas : "summary , Assignee")
    /// </summary>
    [HttpGet("BugRejectCycle")]
    public async Task<ActionResult<ResponseDto>> GetBugRejectCycleAsync([FromQuery] bool unAssigned = true,
        int top = 10)
    {
        var result = await _repo.GetRejectedBugCycleAsync(unAssigned, top);
        var response = GenerateResponse(HttpStatusCode.OK, "", BugText.BugRejectCycle.Title,
            BugText.BugRejectCycle.Description, result, result.Count);
        return Ok(response);
    }

    /// <summary>
    /// Table : Table Of All Bugs (Key,Summary,status,reporter,Assignee,labels,created,age)
    /// </summary>
    [HttpGet("BugTable")]
    public async Task<ActionResult<ResponseDto>> GetBugTableAsync([FromQuery] string? statusFilter = null,
        string sortBy = "Key", bool sortDescending = true, int page = 1, int pageSize = 15)
    {
        var result = await _repo.GetAllBugsTableAsync(statusFilter, sortBy, sortDescending, page, pageSize);
        
        var s = result.Skip((page - 1) * pageSize)
            .Take(pageSize).ToList();
        
        var response = GenerateResponse(HttpStatusCode.OK, "", BugText.BugTable.Title, BugText.BugTable.Description,
            s, result.Count, page, pageSize);
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