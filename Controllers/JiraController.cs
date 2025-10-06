using JiraDashboard.Dtos;
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
            var result = await _repo.GetAssigneeIssueDetailsAsync();
            return Ok(result);
        }
}