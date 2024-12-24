namespace backend.DTOs
{
    public class SessionCreateRequestDTO
    {
        public int GameId { get; set; }
        public int PlayerId { get; set; }
        public int Duration { get; set; }
    }

    public class SessionResponseDTO
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public int PlayerId { get; set; }
        public int Duration { get; set; }
        public int StartRange { get; set; }
        public int EndRange { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Score { get; set; }
    }

    public class SessionUpdateRequestDTO
    {
        public int SessionId { get; set; }
        public int Score { get; set; }
    }
}
