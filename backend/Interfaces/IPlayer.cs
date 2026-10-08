using backend.DTOs;
using backend.Models;

namespace backend.Interfaces
{
    public interface IPlayer
    {
        Task<Player> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

        Task<Player?> ValidateCredentialsAsync(LoginRequest request, CancellationToken cancellationToken = default);

        Task<Player?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<Player?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    }
}
