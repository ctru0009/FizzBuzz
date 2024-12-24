using backend.DTOs;
using backend.Models;

namespace backend.Interfaces
{
    public interface IGame
    {
        Task<GameResponseDTO> CreateGame(GameRequestDTO game);
        Task<Game> GetGame(int id);
        Task<List<GameResponseDTO>> GetGames();
        Task<bool> ValidateAnswer(GameAnswerSubmit gameAnswerSubmit);
        Task<Game> UpdateGame(Game game);
        Task<Game> DeleteGame(int id);
        int GenerateRandomNumber(int sessionId, int[] usedNumbers, int startRange, int endRange);
    }
}
