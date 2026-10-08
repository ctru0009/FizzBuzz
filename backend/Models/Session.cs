using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Session
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int GameId { get; set; }

        public Game? Game { get; set; }

        public SessionStatus Status { get; set; } = SessionStatus.Open;

        public int DurationSeconds { get; set; }

        public DateTime StartTimeUtc { get; set; }

        public DateTime EndTimeUtc { get; set; }

        public int CurrentNumber { get; set; }

        public int CurrentRound { get; set; } = 1;

        public ICollection<SessionParticipant> Participants { get; set; } = [];

        public ICollection<SessionAnswer> Answers { get; set; } = [];
    }
}
