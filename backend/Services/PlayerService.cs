using backend.Data;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class PlayerService : IPlayer
    {
        private readonly BackendAppDbContext _context;

        public PlayerService(BackendAppDbContext context)
        {
            _context = context;
        }

        public async Task<Player> CreatePlayer(PlayerRequestDTO player)
        {
            var newPlayer = new Player
            {
                Name = player.Name,
                CreatedAt = DateTime.UtcNow,
                TotalScores = 0,
                TotalGamesPlayed = 0
            };
            try
            {
                await _context.Players.AddAsync(newPlayer);
                await _context.SaveChangesAsync();
                return newPlayer;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task<Player?> GetPlayer(int id)
        {
            try
            {
                var player = await _context.Players.FindAsync(id);
                return player ?? throw new Exception("Player not found");
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task<Player?> GetPlayerByName(string name)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Name == name);
            return player;
        }

        public async Task<Player> UpdatePlayer(Player player)
        {
            try
            {
                _context.Players.Update(player);
                await _context.SaveChangesAsync();
                return player;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}
