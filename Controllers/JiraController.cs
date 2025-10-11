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

        [HttpGet("UserBarChart")]
        public async Task<ActionResult<List<UserBarChartDto>>> GetUserBarChart()
        {
            var result = await _repo.GetUserBatChartAsync();
            return Ok(result);
        }
        
        [HttpGet("UserIssueCount")]
        public async Task<ActionResult<List<UserIssueCountDto>>> GetUserIssueCountChart([FromQuery] QueryObject query)
        {
            return await _repo.GetUserIssueCountAsync(query);
        }

        [HttpGet("IssueTypeCount")]
        public async Task<ActionResult<List<IssueTypeCountDto>>> GetIssueTypeCount()
        {
            return await _repo.GetIssueTypeCountAsync();
        }
}