using System.Net;
using JiraDashboard.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace JiraDashboard.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GanttController : ControllerBase
{

    private readonly IGanttService _ganttService;

    public GanttController(IGanttService repo)
    {
        _ganttService = repo;
    }
   
    [HttpGet("EpicGantt")]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto>> GetEpicGanttAsync()
    {
        var result = await _ganttService.GetEpicGanttDataAsync();
        var response = GenerateResponse(HttpStatusCode.OK, "", "Epic Gantt Chart", "Timeline of Epics", result, result.Count);
        return Ok(response);
    }   
    
    [HttpGet("EpicDetail")]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto>> GetEpicDetailAsync([FromQuery] long epicId)
    {
        var result = await _ganttService.GetEpicDetailsGanttAsync(epicId);
        var response = GenerateResponse(HttpStatusCode.OK, "", "Epic Gantt Chart", "Timeline of Epics", result, 1);
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

