using backend.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SessionsController : ControllerBase
    {
        private readonly Interfaces.ISession _sessionService;

        public SessionsController(Interfaces.ISession sessionService)
        {
            _sessionService = sessionService;
        }

        // POST api/sessions/
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SessionCreateRequestDTO sessionDTO)
        {
            try
            {
                var session = await _sessionService.CreateSession(sessionDTO);
                return Ok(session);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // GET api/sessions/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var session = await _sessionService.GetSession(id);
                return Ok(session);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // PUT api/sessions/id
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] SessionUpdateRequestDTO sessionDTO)
        {
            try
            {
                sessionDTO.SessionId = id;
                var session = await _sessionService.UpdateSession(sessionDTO);
                return Ok(session);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

    }
}
