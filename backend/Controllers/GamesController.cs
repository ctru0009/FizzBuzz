using backend.DTOs;
using backend.Interfaces;
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
        public async Task<IActionResult> Post([FromBody] GameRequestDTO gameDTO)
        {
            try
            {
                var game = await _gameService.CreateGame(gameDTO);
                return Ok(game);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // GET api/games
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var games = await _gameService.GetGames();
                return Ok(games);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

    }
}
