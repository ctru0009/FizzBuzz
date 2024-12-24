namespace backend.DTOs
{
    public class GameRequestDTO
    {
        public required string Name { get; set; }

        public int PlayerId { get; set; }
        public required string AuthorName { get; set; }
        public int StartRange { get; set; }
        public int EndRange { get; set; }

        public required RuleDTO[] Rules { get; set; }
    }

    public class GameResponseDTO
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string AuthorName { get; set; }
        public int StartRange { get; set; }
        public int EndRange { get; set; }
        public DateTime CreatedAt { get; set; }
        public required RuleDTO[] Rules { get; set; }
    }

    public class GameAnswerResponse
    {
        public bool IsCorrect { get; set; }
        public int NextNumber { get; set; }
        public int Score { get; set; }

    }

    public class GameAnswerSubmit
    {
        public int SessionId { get; set; }
        public int GameId { get; set; }
        public int Number { get; set; }
        public int StartRange { get; set; }
        public int EndRange { get; set; }
        public required string Answer { get; set; }
        public int Score { get; set; }
    }
}
