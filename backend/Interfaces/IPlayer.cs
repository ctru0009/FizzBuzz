using backend.DTOs;
using backend.Models;

namespace backend.Interfaces
{
    public interface IPlayer
    {
        Task<Player> CreatePlayer(PlayerRequestDTO player);
        Task<Player?> GetPlayer(int id);
        Task<Player?> GetPlayerByName(string name);
        Task<Player> UpdatePlayer(Player player);
    }
}
