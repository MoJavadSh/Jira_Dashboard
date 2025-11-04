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
        public async Task<ActionResult<ResponseDto>> GetUserBarChart([FromQuery] bool unAssign)
        {
            var result = await _repo.GetUserBatChartAsync(unAssign);
            var response = GenerateResponse(HttpStatusCode.OK,"", result, result.Count);
            return Ok(response);

        }
        
        /// <summary>
        /// Chart : Issue Count of each Assignee
        /// </summary>
        [HttpGet("UserIssueCount")]
        public async Task<ActionResult<List<UserIssueCountDto>>> GetUserIssueCountChart([FromQuery] QueryObject query)
        {
            var result = await _repo.GetUserIssueCountAsync(query);
            var response = GenerateResponse(HttpStatusCode.OK, "", result, result.Count);
            return Ok(response);
        }

        /// <summary>
        /// Chart : count of each IssueType
        /// </summary>
        [HttpGet("IssueTypeCount")]
        public async Task<ActionResult<List<IssueTypeCountDto>>> GetIssueTypeCount([FromQuery] QueryObject query)
        {
            var result = await _repo.GetIssueTypeCountAsync(query);
            var response = GenerateResponse(HttpStatusCode.OK, "", result, result.Count);
            return Ok(response);
        }
    
        /// <summary>
        /// Chart : Progress. Of each issue type
        /// </summary>
        [HttpGet("IssueTypeProgress")]
        public async Task<ActionResult<List<IssueTypeProgressDto>>> GetIssueTypeProgressChart([FromQuery] QueryObject query)
        {
            var result = await _repo.GetIssueTypeProgressAsync(query);
            var response = GenerateResponse(HttpStatusCode.OK, "", result, result.Count);
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