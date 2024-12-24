namespace backend.DTOs
{
    public class PlayerRequestDTO
    {
        public required string Name { get; set; }
    }

    public class PlayerResponseDTO
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalScores { get; set; }
        public int TotalGamesPlayed { get; set; }
    }
}
