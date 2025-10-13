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
        public async Task<ActionResult<List<UserBarChartDto>>> GetUserBarChart()
        {
            var result = await _repo.GetUserBatChartAsync();
            return Ok(result);
        }
        
        /// <summary>
        /// Chart : Issue Count of each Assignee
        /// </summary>
        [HttpGet("UserIssueCount")]
        public async Task<ActionResult<List<UserIssueCountDto>>> GetUserIssueCountChart([FromQuery] QueryObject query)
        {
            return await _repo.GetUserIssueCountAsync(query);
        }

        /// <summary>
        /// Chart : count of each IssueType
        /// </summary>
        [HttpGet("IssueTypeCount")]
        public async Task<ActionResult<List<IssueTypeCountDto>>> GetIssueTypeCount([FromQuery] QueryObject query)
        {
            return await _repo.GetIssueTypeCountAsync(query);
        }
    
        /// <summary>
        /// Chart : Progress. Of each issue type
        /// </summary>
        [HttpGet("IssueTypeProgress")]
        public async Task<ActionResult<List<IssueTypeProgressDto>>> GetIssueTypeProgressChart([FromQuery] QueryObject query)
        {
            return await _repo.GetIssueTypeProgressAsync(query);
        }
}