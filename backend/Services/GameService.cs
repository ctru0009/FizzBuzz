using backend.Data;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public sealed class GameService : IGame
    {
        private readonly BackendAppDbContext _context;
        private readonly ILogger<GameService> _logger;

        public GameService(BackendAppDbContext context, ILogger<GameService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<GameResponseDTO> CreateGameAsync(GameRequestDTO game, int playerId, string authorName, CancellationToken cancellationToken = default)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken)
                ?? throw new KeyNotFoundException("Player not found.");

            var newGame = new Game
            {
                Name = game.Name,
                AuthorName = authorName,
                PlayerId = player.Id,
                StartRange = game.StartRange,
                EndRange = game.EndRange,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Games.Add(newGame);
            await _context.SaveChangesAsync(cancellationToken);

            var ruleDTOs = new List<RuleDTO>();
            foreach (var rule in game.Rules)
            {
                _context.Rules.Add(new Rule
                {
                    GameId = newGame.Id,
                    DivisibleBy = rule.DivisibleBy,
                    ReplacementWord = rule.ReplacementWord,
                });
                ruleDTOs.Add(new RuleDTO
                {
                    DivisibleBy = rule.DivisibleBy,
                    ReplacementWord = rule.ReplacementWord,
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created game {GameId} by player {PlayerId}.", newGame.Id, player.Id);

            return new GameResponseDTO
            {
                Id = newGame.Id,
                Name = newGame.Name,
                AuthorName = newGame.AuthorName,
                StartRange = newGame.StartRange,
                EndRange = newGame.EndRange,
                CreatedAt = newGame.CreatedAt,
                Rules = ruleDTOs.ToArray(),
            };
        }

        public async Task<GameResponseDTO?> GetGameAsync(int id, CancellationToken cancellationToken = default)
        {
            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
            if (game is null)
            {
                return null;
            }

            var rules = await GetRuleDTOsAsync(game.Id, cancellationToken);
            return ToResponse(game, rules);
        }

        public async Task<List<GameResponseDTO>> GetGamesAsync(CancellationToken cancellationToken = default)
        {
            var games = await _context.Games.OrderBy(g => g.Id).ToListAsync(cancellationToken);
            var response = new List<GameResponseDTO>(games.Count);
            foreach (var game in games)
            {
                var rules = await GetRuleDTOsAsync(game.Id, cancellationToken);
                response.Add(ToResponse(game, rules));
            }

            return response;
        }

        public async Task<bool> UpdateGameAsync(int id, GameRequestDTO game, CancellationToken cancellationToken = default)
        {
            var existing = await _context.Games.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
            if (existing is null)
            {
                return false;
            }

            existing.Name = game.Name;
            existing.StartRange = game.StartRange;
            existing.EndRange = game.EndRange;

            var oldRules = await _context.Rules.Where(r => r.GameId == id).ToListAsync(cancellationToken);
            _context.Rules.RemoveRange(oldRules);
            foreach (var rule in game.Rules)
            {
                _context.Rules.Add(new Rule
                {
                    GameId = id,
                    DivisibleBy = rule.DivisibleBy,
                    ReplacementWord = rule.ReplacementWord,
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> DeleteGameAsync(int id, CancellationToken cancellationToken = default)
        {
            var existing = await _context.Games.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
            if (existing is null)
            {
                return false;
            }

            _context.Games.Remove(existing);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<RuleDTO[]> GetRuleDTOsAsync(int gameId, CancellationToken cancellationToken)
        {
            return await _context.Rules
                .Where(r => r.GameId == gameId)
                .OrderBy(r => r.Id)
                .Select(r => new RuleDTO { DivisibleBy = r.DivisibleBy, ReplacementWord = r.ReplacementWord })
                .ToArrayAsync(cancellationToken);
        }

        private static GameResponseDTO ToResponse(Game game, RuleDTO[] rules)
        {
            return new GameResponseDTO
            {
                Id = game.Id,
                Name = game.Name,
                AuthorName = game.AuthorName,
                StartRange = game.StartRange,
                EndRange = game.EndRange,
                CreatedAt = game.CreatedAt,
                Rules = rules,
            };
        }
    }
}
