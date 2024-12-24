using backend.DTOs;

namespace backend.Interfaces
{
    public interface ISession
    {
        Task<SessionResponseDTO> CreateSession(SessionCreateRequestDTO session);
        Task<SessionResponseDTO> GetSession(int id);
        Task<SessionResponseDTO> UpdateSession(SessionUpdateRequestDTO session);
    }
}
