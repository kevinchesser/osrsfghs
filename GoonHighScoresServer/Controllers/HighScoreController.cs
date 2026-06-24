using GoonHighScoresServer.Exceptions;
using GoonHighScoresServer.Interfaces;
using GoonHighScoresServer.Models;
using Microsoft.AspNetCore.Mvc;

namespace GoonHighScoresServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HighScoreController : ControllerBase
    {
        private readonly IHighScoreService _highScoreService;

        public HighScoreController(IHighScoreService highScoreService)
        {
            _highScoreService = highScoreService;
        }

        [HttpGet("{characterName}")]
        public async Task<IActionResult> GetCharacterOverview([FromRoute]string characterName)
        {
            try
            {
                CharacterOverview characterOverview = await _highScoreService.GetCharacterOverview(characterName);
                return Ok(characterOverview);
            }
            catch(Exception ex)
            {
                if (ex is CharacterNotFoundException exception)
                {
                    return NotFound(exception.Message);
                }

                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("last24HourLeaderboard")]
        public async Task<IActionResult> Last24HourLeaderboard()
        {
            TimespanXpLeaderboardViewModel timespanXpLeaderboardViewModel = await _highScoreService.GetLastXTimeSpanOverallXpLeadboard(TimeSpan.FromHours(24));
            return Ok(timespanXpLeaderboardViewModel);
        }
    }
}
