using System.Net;
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
        var response = GenerateResponse(HttpStatusCode.OK,"", result, result.Count);
        return Ok(response);

    }
    
    /// <summary>
    /// Header : Status of all bugs
    /// </summary>
    [HttpGet("BugStatus")]
    public async Task<ActionResult<ResponseDto>> GetBugStatus()
    {
        var result = await _repo.GetBugStatus();
        var response = GenerateResponse(HttpStatusCode.OK,"", result, 1);
        return Ok(response);

    }
    
    private static ResponseDto GenerateResponse(HttpStatusCode statusCode, string message, object? result = null,
        int total = 0, int page = 1, int perPage = 10)
        => new()
        {
            StatusCode = (int)statusCode,
            Message = new List<string>() { message },
            Result = result,
            Total = total,
            Page = page,
            PerPage = perPage
        };
}