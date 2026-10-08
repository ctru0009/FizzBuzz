using backend.Auth;
using backend.DTOs;
using backend.Gameplay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace backend.Hubs
{
    [Authorize]
    public sealed class GameSessionHub : Hub
    {
        private readonly IGameSessionStore _store;
        private readonly IGameRuleService _rules;
        private readonly ILogger<GameSessionHub> _logger;

        public GameSessionHub(IGameSessionStore store, IGameRuleService rules, ILogger<GameSessionHub> logger)
        {
            _store = store;
            _rules = rules;
            _logger = logger;
        }

        public async Task<JoinSessionResult> JoinSession(int sessionId)
        {
            var playerId = GetPlayerId();
            JoinSessionData joined;
            try
            {
                joined = await _store.JoinSessionAsync(sessionId, playerId);
            }
            catch (KeyNotFoundException e)
            {
                throw new HubException(e.Message);
            }
            catch (InvalidOperationException e)
            {
                throw new HubException(e.Message);
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, SessionGroups.Name(sessionId));
            _logger.LogInformation("Player {PlayerId} joined session {SessionId}.", playerId, sessionId);

            var snapshot = joined.Snapshot;
            return new JoinSessionResult(
                snapshot.Id,
                snapshot.CurrentNumber,
                snapshot.CurrentRound,
                snapshot.EndTimeUtc,
                snapshot.Scores,
                joined.AnsweredCurrentRound);
        }

        public async Task<SubmitAnswerResult> SubmitAnswer(int sessionId, int round, string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
            {
                throw new HubException("Answer is required.");
            }

            if (answer.Length > 100)
            {
                throw new HubException("Answer must be at most 100 characters.");
            }

            var playerId = GetPlayerId();

            var snapshot = await _store.GetSnapshotAsync(sessionId);
            if (snapshot is null)
            {
                throw new HubException(HubErrors.SessionNotFound);
            }

            if (snapshot.Status != Models.SessionStatus.Open)
            {
                throw new HubException(HubErrors.SessionFinished);
            }

            if (!await _store.IsParticipantAsync(sessionId, playerId))
            {
                throw new HubException(HubErrors.NotJoined);
            }

            if (round != snapshot.CurrentRound)
            {
                throw new HubException(HubErrors.OldRound);
            }

            if (await _store.HasAnsweredAsync(sessionId, playerId, round))
            {
                throw new HubException(HubErrors.AlreadyAnswered);
            }

            var isCorrect = await _rules.ValidateAnswerAsync(snapshot.GameId, snapshot.CurrentNumber, answer);
            var score = 0;
            try
            {
                score = await _store.RecordAnswerAsync(sessionId, playerId, round, isCorrect);
            }
            catch (InvalidOperationException e)
            {
                throw new HubException(e.Message);
            }
            catch (KeyNotFoundException e)
            {
                throw new HubException(e.Message);
            }

            var latest = await _store.GetSnapshotAsync(sessionId) ?? snapshot;
            await Clients.Group(SessionGroups.Name(sessionId))
                .SendAsync(HubMessages.ScoresUpdated, new ScoresUpdatedPayload(latest.Scores));
            return new SubmitAnswerResult(isCorrect, score);
        }

        public async Task LeaveSession(int sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, SessionGroups.Name(sessionId));
            _logger.LogInformation("Connection {ConnectionId} left session {SessionId}.", Context.ConnectionId, sessionId);
        }

        private int GetPlayerId()
        {
            return Context.User?.GetPlayerId()
                ?? throw new HubException("Missing player identity.");
        }
    }
}
