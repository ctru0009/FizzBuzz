using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace backend.Models
{
    [Index(nameof(SessionId), nameof(PlayerId), nameof(Round), IsUnique = true)]
    public class SessionAnswer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int SessionId { get; set; }

        public Session? Session { get; set; }

        public int PlayerId { get; set; }

        public Player? Player { get; set; }

        public int Round { get; set; }

        public bool IsCorrect { get; set; }

        public DateTime AnsweredAt { get; set; }
    }
}
