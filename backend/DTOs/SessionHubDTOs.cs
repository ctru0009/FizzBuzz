namespace backend.DTOs
{
    public sealed record JoinSessionResult(
        int SessionId,
        int Number,
        int Round,
        DateTime EndsAtUtc,
        PlayerScore[] Scores,
        bool AnsweredCurrentRound);

    public sealed record SubmitAnswerResult(bool Correct, int Score);

    public sealed record NumberAdvancedPayload(int Number, int Round, DateTime EndsAtUtc, DateTime AdvancesAtUtc);

    public sealed record ScoresUpdatedPayload(PlayerScore[] Scores);

    public sealed record SessionEndedPayload(PlayerScore[] FinalScores);
}
