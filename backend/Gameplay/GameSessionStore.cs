using backend.Data;
using backend.DTOs;
using backend.Interfaces.Cache;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Gameplay
{
    public sealed class GameSessionStore : IGameSessionStore
    {
        private static readonly TimeSpan RedisGracePeriod = TimeSpan.FromMinutes(5);

        private readonly BackendAppDbContext _context;
        private readonly IGameRuleService _rules;
        private readonly IRedisCachingService _redis;

        public GameSessionStore(BackendAppDbContext context, IGameRuleService rules, IRedisCachingService redis)
        {
            _context = context;
            _rules = rules;
            _redis = redis;
        }

        public async Task<SessionSnapshot> CreateSessionAsync(int gameId, int creatorPlayerId, int durationSeconds, CancellationToken cancellationToken = default)
        {
            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken)
                ?? throw new KeyNotFoundException("Game not found.");
            var creator = await _context.Players.FirstOrDefaultAsync(p => p.Id == creatorPlayerId, cancellationToken)
                ?? throw new KeyNotFoundException("Player not found.");

            var now = DateTime.UtcNow;
            var number = _rules.PickNumber(game.StartRange, game.EndRange, []);
            var session = new Session
            {
                GameId = gameId,
                Status = SessionStatus.Open,
                DurationSeconds = durationSeconds,
                StartTimeUtc = now,
                EndTimeUtc = now.AddSeconds(durationSeconds),
                CurrentNumber = number,
                CurrentRound = 1,
            };
            _context.Sessions.Add(session);
            _context.SessionParticipants.Add(new SessionParticipant
            {
                Session = session,
                PlayerId = creatorPlayerId,
                Score = 0,
                JoinedAt = now,
            });
            await _context.SaveChangesAsync(cancellationToken);
            await _redis.AddUsedNumberAsync(session.Id, number, TimeSpan.FromSeconds(durationSeconds) + RedisGracePeriod, cancellationToken);

            return new SessionSnapshot(
                session.Id,
                gameId,
                session.Status,
                session.StartTimeUtc,
                session.EndTimeUtc,
                session.CurrentNumber,
                session.CurrentRound,
                [new PlayerScore(creator.Id, creator.Name, 0)]);
        }

        public async Task<JoinSessionData> JoinSessionAsync(int sessionId, int playerId, CancellationToken cancellationToken = default)
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
                ?? throw new KeyNotFoundException("Session not found.");
            if (session.Status == SessionStatus.Finished)
            {
                throw new InvalidOperationException("Session has finished.");
            }

            var participant = await _context.SessionParticipants
                .FirstOrDefaultAsync(p => p.SessionId == sessionId && p.PlayerId == playerId, cancellationToken);
            if (participant is null)
            {
                _context.SessionParticipants.Add(new SessionParticipant
                {
                    SessionId = sessionId,
                    PlayerId = playerId,
                    Score = 0,
                    JoinedAt = DateTime.UtcNow,
                });
                await _context.SaveChangesAsync(cancellationToken);
            }

            var snapshot = await BuildSnapshotAsync(session, cancellationToken);
            var answered = await HasAnsweredAsync(sessionId, playerId, snapshot.CurrentRound, cancellationToken);
            return new JoinSessionData(snapshot, answered);
        }

        public async Task<SessionSnapshot?> GetSnapshotAsync(int sessionId, CancellationToken cancellationToken = default)
        {
            var session = await _context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
            if (session is null)
            {
                return null;
            }

            return await BuildSnapshotAsync(session, cancellationToken);
        }

        public async Task<IReadOnlyList<OpenSessionInfo>> ListOpenSessionsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Sessions
                .Where(s => s.Status == SessionStatus.Open)
                .OrderBy(s => s.Id)
                .Select(s => new OpenSessionInfo(s.Id, s.GameId, s.Game!.Name, s.Participants.Count, s.EndTimeUtc))
                .ToArrayAsync(cancellationToken);
        }

        public Task<bool> IsParticipantAsync(int sessionId, int playerId, CancellationToken cancellationToken = default)
        {
            return _context.SessionParticipants
                .AnyAsync(p => p.SessionId == sessionId && p.PlayerId == playerId, cancellationToken);
        }

        public Task<bool> HasAnsweredAsync(int sessionId, int playerId, int round, CancellationToken cancellationToken = default)
        {
            return _context.SessionAnswers
                .AnyAsync(a => a.SessionId == sessionId && a.PlayerId == playerId && a.Round == round, cancellationToken);
        }

        public async Task<int> RecordAnswerAsync(int sessionId, int playerId, int round, bool isCorrect, CancellationToken cancellationToken = default)
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
                ?? throw new KeyNotFoundException("Session not found.");
            if (session.Status == SessionStatus.Finished)
            {
                throw new InvalidOperationException("Session has finished.");
            }

            var participant = await _context.SessionParticipants
                .FirstOrDefaultAsync(p => p.SessionId == sessionId && p.PlayerId == playerId, cancellationToken)
                ?? throw new InvalidOperationException("Join the session before answering.");
            if (await HasAnsweredAsync(sessionId, playerId, round, cancellationToken))
            {
                throw new InvalidOperationException("Already answered this round.");
            }

            _context.SessionAnswers.Add(new SessionAnswer
            {
                SessionId = sessionId,
                PlayerId = playerId,
                Round = round,
                IsCorrect = isCorrect,
                AnsweredAt = DateTime.UtcNow,
            });
            if (isCorrect)
            {
                participant.Score += 10;
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Race with a concurrent submit for the same round. The unique
                // (SessionId, PlayerId, Round) constraint rejects the second row.
                if (await HasAnsweredAsync(sessionId, playerId, round, cancellationToken))
                {
                    throw new InvalidOperationException("Already answered this round.");
                }

                throw;
            }

            return participant.Score;
        }

        public async Task<IReadOnlyList<OpenSessionState>> ListOpenSessionStatesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Sessions
                .AsNoTracking()
                .Where(s => s.Status == SessionStatus.Open)
                .OrderBy(s => s.Id)
                .Select(s => new OpenSessionState(s.Id, s.GameId, s.Game!.StartRange, s.Game!.EndRange, s.CurrentRound, s.EndTimeUtc))
                .ToArrayAsync(cancellationToken);
        }

        public async Task<SessionAdvanced?> AdvanceSessionAsync(int sessionId, int newNumber, CancellationToken cancellationToken = default)
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
            if (session is null || session.Status == SessionStatus.Finished)
            {
                return null;
            }

            session.CurrentNumber = newNumber;
            session.CurrentRound += 1;
            await _context.SaveChangesAsync(cancellationToken);
            return new SessionAdvanced(session.CurrentNumber, session.CurrentRound, session.EndTimeUtc);
        }

        public async Task<PlayerScore[]?> FinishSessionAsync(int sessionId, CancellationToken cancellationToken = default)
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
            if (session is null)
            {
                return null;
            }

            if (session.Status != SessionStatus.Finished)
            {
                session.Status = SessionStatus.Finished;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return (await BuildSnapshotAsync(session, cancellationToken)).Scores;
        }

        private async Task<SessionSnapshot> BuildSnapshotAsync(Session session, CancellationToken cancellationToken)
        {
            var scores = await _context.SessionParticipants
                .AsNoTracking()
                .Where(p => p.SessionId == session.Id)
                .OrderByDescending(p => p.Score)
                .ThenBy(p => p.Player!.Name)
                .Select(p => new PlayerScore(p.PlayerId, p.Player!.Name, p.Score))
                .ToArrayAsync(cancellationToken);

            return new SessionSnapshot(
                session.Id,
                session.GameId,
                session.Status,
                session.StartTimeUtc,
                session.EndTimeUtc,
                session.CurrentNumber,
                session.CurrentRound,
                scores);
        }
    }
}
