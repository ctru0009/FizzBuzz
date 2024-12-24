using backend.Data;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class GameService : IGame
    {
        private readonly BackendAppDbContext _context;
        private readonly ILogger<GameService> _logger;
        public GameService(BackendAppDbContext context, ILogger<GameService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<GameResponseDTO> CreateGame(GameRequestDTO game)
        {
            try
            {
                var player = _context.Players.FirstOrDefault(p => p.Id == game.PlayerId);
                if (player == null)
                {
                    throw new Exception("Player not found");
                }
                // Create the game
                var newGame = new Game
                {
                    Name = game.Name,
                    AuthorName = game.AuthorName,
                    PlayerId = game.PlayerId,
                    StartRange = game.StartRange,
                    EndRange = game.EndRange,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Games.Add(newGame);
                _context.SaveChanges();
                var gameResponse = new GameResponseDTO
                {
                    Id = newGame.Id,
                    Name = newGame.Name,
                    AuthorName = player.Name,
                    StartRange = newGame.StartRange,
                    EndRange = newGame.EndRange,
                    CreatedAt = newGame.CreatedAt,
                    Rules = new List<RuleDTO>().ToArray()
                };
                // Create the rules
                var ruleDTOs = new List<RuleDTO>();
                foreach (var rule in game.Rules)
                {
                    var newRule = new Rule
                    {
                        GameId = newGame.Id,
                        DivisibleBy = rule.DivisibleBy,
                        ReplacementWord = rule.ReplacementWord
                    };
                    _context.Rules.Add(newRule);
                    ruleDTOs.Add(new RuleDTO
                    {
                        DivisibleBy = newRule.DivisibleBy,
                        ReplacementWord = newRule.ReplacementWord
                    });
                }
                gameResponse.Rules = ruleDTOs.ToArray();
                await _context.SaveChangesAsync();
                return gameResponse;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public Task<Game> DeleteGame(int id)
        {
            throw new NotImplementedException();
        }

        public Task<Game> GetGame(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<List<GameResponseDTO>> GetGames()
        {
            try
            {
                var games = await _context.Games.ToListAsync();
                var gameResponse = new List<GameResponseDTO>();

                foreach (var game in games)
                {
                    var rules = await _context.Rules.Where(r => r.GameId == game.Id).ToListAsync();
                    var ruleDTOs = new List<RuleDTO>();
                    foreach (var rule in rules)
                    {
                        ruleDTOs.Add(new RuleDTO
                        {
                            DivisibleBy = rule.DivisibleBy,
                            ReplacementWord = rule.ReplacementWord
                        });
                    }
                    var player = await _context.Players.FindAsync(game.PlayerId);

                    if (player == null)
                    {
                        throw new Exception("Player not found");
                    }

                    gameResponse.Add(new GameResponseDTO
                    {
                        Id = game.Id,
                        Name = game.Name,
                        AuthorName = game.AuthorName,
                        StartRange = game.StartRange,
                        EndRange = game.EndRange,
                        CreatedAt = game.CreatedAt,
                        Rules = ruleDTOs.ToArray()
                    });
                }
                return gameResponse;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task<bool> ValidateAnswer(GameAnswerSubmit gameAnswerSubmit)
        {
            try
            {
                // Check answer against rules
                var rules = await _context.Rules.Where(r => r.GameId == gameAnswerSubmit.GameId).ToListAsync();
                rules = _context.Rules.Where(r => r.GameId == gameAnswerSubmit.GameId).ToList();
                var isCorrect = false;
                var answer = "";

                _logger.LogInformation("Number: " + gameAnswerSubmit.Number);
                _logger.LogInformation("rules: " + rules.Count.ToString());
                _logger.LogInformation("gameId: " + gameAnswerSubmit.GameId.ToString());


                // Generate the answer string
                foreach (var rule in rules)
                {
                    if (gameAnswerSubmit.Number % rule.DivisibleBy == 0)
                    {
                        answer += rule.ReplacementWord;
                    }
                }
                _logger.LogInformation("Answer: " + answer);
                // Check if the answer is correct
                // If the answer is empty and the number is the same as the answer, then it is correct
                isCorrect = (string.IsNullOrEmpty(answer) && gameAnswerSubmit.Answer.Equals(gameAnswerSubmit.Number.ToString())) ||
                            answer.Equals(gameAnswerSubmit.Answer, StringComparison.OrdinalIgnoreCase);


                return isCorrect;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public int GenerateRandomNumber(int sessionId, int[] usedNumbers, int startRange, int endRange)
        {
            try
            {
                var random = new Random(sessionId);
                int nextNumber = 0;
                do
                {
                    nextNumber = random.Next(startRange, endRange);
                } while (usedNumbers.Contains(nextNumber));
                return nextNumber;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public Task<Game> UpdateGame(Game game)
        {
            throw new NotImplementedException();
        }
    }
}
