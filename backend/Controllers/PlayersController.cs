using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayersController(IPlayer playerService) : Controller
    {
        private readonly IPlayer _playerService = playerService;

        // POST api/player/login
        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] PlayerRequestDTO playerDTO)
        {
            try
            {
                var player = await _playerService.GetPlayerByName(playerDTO.Name);
                if (player == null)
                {
                    player = await _playerService.CreatePlayer(playerDTO);
                }

                return Ok(player);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


    }
}
