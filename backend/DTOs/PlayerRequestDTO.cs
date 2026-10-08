using System.ComponentModel.DataAnnotations;

namespace backend.DTOs
{
    public class RegisterRequest
    {
        [Required]
        [MinLength(1)]
        [MaxLength(50)]
        public required string Name { get; set; }

        [Required]
        [MinLength(8)]
        [MaxLength(100)]
        public required string Password { get; set; }
    }

    public class LoginRequest
    {
        [Required]
        [MinLength(1)]
        [MaxLength(50)]
        public required string Name { get; set; }

        [Required]
        [MinLength(8)]
        [MaxLength(100)]
        public required string Password { get; set; }
    }

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
