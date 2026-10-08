using backend.Data;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class PlayerService : IPlayer
    {
        private readonly BackendAppDbContext _context;
        private readonly IPasswordHasher<Player> _passwordHasher;

        public PlayerService(BackendAppDbContext context, IPasswordHasher<Player> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<Player> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            var existing = await _context.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name == request.Name, cancellationToken);
            if (existing is not null)
            {
                throw new DuplicatePlayerNameException(request.Name);
            }

            var player = new Player
            {
                Name = request.Name,
                PasswordHash = string.Empty,
                CreatedAt = DateTime.UtcNow,
                TotalScores = 0,
                TotalGamesPlayed = 0,
            };
            player.PasswordHash = _passwordHasher.HashPassword(player, request.Password);

            try
            {
                await _context.Players.AddAsync(player, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception, request.Name))
            {
                throw new DuplicatePlayerNameException(request.Name);
            }

            return player;
        }

        public async Task<Player?> ValidateCredentialsAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            var player = await GetByNameAsync(request.Name, cancellationToken);
            if (player is null)
            {
                return null;
            }

            var result = _passwordHasher.VerifyHashedPassword(player, player.PasswordHash, request.Password);
            return result == PasswordVerificationResult.Success ? player : null;
        }

        public async Task<Player?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Players.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<Player?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _context.Players.FirstOrDefaultAsync(p => p.Name == name, cancellationToken);
        }

        private static bool IsUniqueViolation(DbUpdateException exception, string name)
        {
            var text = exception.ToString();
            return text.Contains("IX_Players_Name", StringComparison.OrdinalIgnoreCase)
                || text.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                || text.Contains("unique", StringComparison.OrdinalIgnoreCase)
                || text.Contains($"'{name}'", StringComparison.Ordinal);
        }
    }
}
