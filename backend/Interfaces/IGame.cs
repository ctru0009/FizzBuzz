using backend.DTOs;
using backend.Models;

namespace backend.Interfaces
{
    public interface IGame
    {
        Task<GameResponseDTO> CreateGameAsync(GameRequestDTO game, int playerId, string authorName, CancellationToken cancellationToken = default);
        Task<GameResponseDTO?> GetGameAsync(int id, CancellationToken cancellationToken = default);
        Task<List<GameResponseDTO>> GetGamesAsync(CancellationToken cancellationToken = default);
        Task<bool> UpdateGameAsync(int id, GameRequestDTO game, CancellationToken cancellationToken = default);
        Task<bool> DeleteGameAsync(int id, CancellationToken cancellationToken = default);
    }
}
