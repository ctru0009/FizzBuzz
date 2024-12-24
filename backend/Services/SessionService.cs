using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class SessionService : Interfaces.ISession
    {
        private readonly BackendAppDbContext _context;

        public SessionService(BackendAppDbContext context)
        {
            _context = context;
        }

        public async Task<SessionResponseDTO> CreateSession(SessionCreateRequestDTO session)
        {
            try
            {
                var newSession = new Session
                {
                    PlayerId = session.PlayerId,
                    GameId = session.GameId,
                    Duration = session.Duration,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow.AddSeconds(session.Duration),
                    Score = 0,
                };
                await _context.Sessions.AddAsync(newSession);
                await _context.SaveChangesAsync();
                var response = new SessionResponseDTO
                {
                    Id = newSession.Id,
                    PlayerId = newSession.PlayerId,
                    GameId = newSession.GameId,
                    Duration = newSession.Duration,
                    StartTime = newSession.StartTime,
                    EndTime = newSession.EndTime,
                    Score = newSession.Score,
                };
                return response;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task<SessionResponseDTO> GetSession(int id)
        {
            try
            {
                var session = await _context.Sessions.FindAsync(id);

                if (session == null)
                {
                    throw new Exception("Session not found");
                }

                // Find 
                var game = await _context.Games.FindAsync(session.GameId);
                if (game == null)
                {
                    throw new Exception("Game not found");
                }


                var response = new SessionResponseDTO
                {
                    Id = session.Id,
                    PlayerId = session.PlayerId,
                    GameId = session.GameId,
                    Duration = session.Duration,
                    StartRange = game.StartRange,
                    EndRange = game.EndRange,
                    StartTime = session.StartTime,
                    EndTime = session.EndTime,
                    Score = session.Score,
                };
                return response;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task<SessionResponseDTO> UpdateSession(SessionUpdateRequestDTO session)
        {
            try
            {
                var sessionToUpdate = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == session.SessionId);

                if (sessionToUpdate == null)
                {
                    throw new Exception("Session not found");
                }

                sessionToUpdate.Score = session.Score;
                await _context.SaveChangesAsync();

                var response = new SessionResponseDTO
                {
                    Id = sessionToUpdate.Id,
                    PlayerId = sessionToUpdate.PlayerId,
                    GameId = sessionToUpdate.GameId,
                    Duration = sessionToUpdate.Duration,
                    StartTime = sessionToUpdate.StartTime,
                    EndTime = sessionToUpdate.EndTime,
                    Score = sessionToUpdate.Score,
                };
                return response;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}
