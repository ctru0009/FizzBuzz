using backend.DTOs;

namespace backend.Gameplay
{
    public interface IGameSessionStore
    {
        // Throws KeyNotFoundException ("Game not found.") when the game is missing.
        Task<SessionSnapshot> CreateSessionAsync(int gameId, int creatorPlayerId, int durationSeconds, CancellationToken cancellationToken = default);

        // Throws KeyNotFoundException ("Session not found.") when missing,
        // InvalidOperationException ("Session has finished.") when finished.
        Task<JoinSessionData> JoinSessionAsync(int sessionId, int playerId, CancellationToken cancellationToken = default);

        Task<SessionSnapshot?> GetSnapshotAsync(int sessionId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<OpenSessionInfo>> ListOpenSessionsAsync(CancellationToken cancellationToken = default);

        // Membership read shared with the auth lane.
        Task<bool> IsParticipantAsync(int sessionId, int playerId, CancellationToken cancellationToken = default);

        Task<bool> HasAnsweredAsync(int sessionId, int playerId, int round, CancellationToken cancellationToken = default);

        // Records one answer and returns the player total score. Correct answers add 10.
        // Throws KeyNotFoundException ("Session not found.") when missing, InvalidOperationException
        // with a contract message when finished, not joined, or already answered.
        Task<int> RecordAnswerAsync(int sessionId, int playerId, int round, bool isCorrect, CancellationToken cancellationToken = default);

        // Ticker support. Advance returns null when missing or finished.
        Task<IReadOnlyList<OpenSessionState>> ListOpenSessionStatesAsync(CancellationToken cancellationToken = default);

        Task<SessionAdvanced?> AdvanceSessionAsync(int sessionId, int newNumber, CancellationToken cancellationToken = default);

        // Idempotent finish. Returns null when missing, else the final scores.
        Task<PlayerScore[]?> FinishSessionAsync(int sessionId, CancellationToken cancellationToken = default);
    }

    public sealed record JoinSessionData(SessionSnapshot Snapshot, bool AnsweredCurrentRound);

    public sealed record OpenSessionState(int SessionId, int GameId, int StartRange, int EndRange, int CurrentRound, DateTime EndTimeUtc);

    public sealed record SessionAdvanced(int Number, int Round, DateTime EndsAtUtc);
}
