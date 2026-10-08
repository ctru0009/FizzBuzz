using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using backend.Models;

namespace backend.DTOs
{
    public class SessionCreateRequest
    {
        [Range(1, int.MaxValue)]
        public int GameId { get; set; }

        [Range(30, 1800)]
        public int DurationSeconds { get; set; }
    }

    public sealed record PlayerScore(int PlayerId, string PlayerName, int Score);

    public sealed record OpenSessionInfo(int Id, int GameId, string GameName, int PlayerCount, DateTime EndsAtUtc);

    public sealed record SessionSnapshot(
        int Id,
        int GameId,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] SessionStatus Status,
        DateTime StartTimeUtc,
        DateTime EndTimeUtc,
        int CurrentNumber,
        int CurrentRound,
        PlayerScore[] Scores);
}
