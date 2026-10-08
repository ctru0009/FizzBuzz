using backend.Auth;
using backend.DTOs;
using backend.Gameplay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SessionsController : ControllerBase
    {
        private readonly IGameSessionStore _store;

        public SessionsController(IGameSessionStore store)
        {
            _store = store;
        }

        // POST api/sessions
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Post([FromBody] SessionCreateRequest request, CancellationToken cancellationToken)
        {
            var playerId = User.GetPlayerId();
            if (playerId is null)
            {
                return Unauthorized(new { error = "Missing player identity." });
            }

            try
            {
                var snapshot = await _store.CreateSessionAsync(request.GameId, playerId.Value, request.DurationSeconds, cancellationToken);
                return Created($"/api/sessions/{snapshot.Id}", snapshot);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { error = e.Message });
            }
        }

        // GET api/sessions/open
        [HttpGet("open")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOpen(CancellationToken cancellationToken)
        {
            var open = await _store.ListOpenSessionsAsync(cancellationToken);
            return Ok(open);
        }

        // GET api/sessions/{id}
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
        {
            var snapshot = await _store.GetSnapshotAsync(id, cancellationToken);
            if (snapshot is null)
            {
                return NotFound(new { error = "Session not found." });
            }

            return Ok(snapshot);
        }
    }
}
