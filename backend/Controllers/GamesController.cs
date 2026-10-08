using backend.Auth;
using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GamesController : ControllerBase
    {
        private readonly IGame _gameService;

        public GamesController(IGame gameService)
        {
            _gameService = gameService;
        }

        // POST api/games
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Post([FromBody] GameRequestDTO gameDTO, CancellationToken cancellationToken)
        {
            var playerId = User.GetPlayerId();
            var authorName = User.GetPlayerName();
            if (playerId is null || authorName is null)
            {
                return Unauthorized(new { error = "Missing player identity." });
            }

            try
            {
                var game = await _gameService.CreateGameAsync(gameDTO, playerId.Value, authorName, cancellationToken);
                return Created($"/api/games/{game.Id}", game);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { error = e.Message });
            }
        }

        // GET api/games
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var games = await _gameService.GetGamesAsync(cancellationToken);
            return Ok(games);
        }
    }
}
