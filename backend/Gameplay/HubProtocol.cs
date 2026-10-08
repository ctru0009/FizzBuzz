namespace backend.Gameplay
{
    internal static class SessionGroups
    {
        public static string Name(int sessionId) => $"session:{sessionId}";
    }

    public static class HubMessages
    {
        public const string NumberAdvanced = "NumberAdvanced";
        public const string ScoresUpdated = "ScoresUpdated";
        public const string SessionEnded = "SessionEnded";
    }

    public static class HubErrors
    {
        public const string SessionNotFound = "Session not found.";
        public const string SessionFinished = "Session has finished.";
        public const string NotJoined = "Join the session before answering.";
        public const string OldRound = "Answer is for an old round.";
        public const string AlreadyAnswered = "Already answered this round.";
    }
}
