using backend.Auth;
using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PlayersController : ControllerBase
    {
        private readonly IPlayer _playerService;
        private readonly TokenService _tokenService;
        private readonly JwtOptions _jwtOptions;

        public PlayersController(IPlayer playerService, TokenService tokenService, IOptions<JwtOptions> jwtOptions)
        {
            _playerService = playerService;
            _tokenService = tokenService;
            _jwtOptions = jwtOptions.Value;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var player = await _playerService.RegisterAsync(request, cancellationToken);
                AuthCookie.Write(HttpContext, _tokenService.GenerateToken(player.Id, player.Name), _jwtOptions);
                return CreatedAtAction(nameof(Me), new AuthResult { Id = player.Id, Name = player.Name });
            }
            catch (DuplicatePlayerNameException)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Name is taken.",
                    Detail = "A player with this name already exists.",
                });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var player = await _playerService.ValidateCredentialsAsync(request, cancellationToken);
            if (player is null)
            {
                return Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Invalid credentials.",
                    Detail = "Name or password is wrong.",
                });
            }

            AuthCookie.Write(HttpContext, _tokenService.GenerateToken(player.Id, player.Name), _jwtOptions);
            return Ok(new AuthResult { Id = player.Id, Name = player.Name });
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public IActionResult Logout()
        {
            AuthCookie.Clear(HttpContext);
            return Ok(new { message = "Logged out." });
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me(CancellationToken cancellationToken)
        {
            var id = User.GetPlayerId();
            if (id is null)
            {
                return Unauthorized();
            }

            var player = await _playerService.GetByIdAsync(id.Value, cancellationToken);
            if (player is null)
            {
                return Unauthorized();
            }

            return Ok(new AuthResult { Id = player.Id, Name = player.Name });
        }
    }
}
