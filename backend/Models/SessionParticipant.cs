using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace backend.Models
{
    [Index(nameof(SessionId), nameof(PlayerId), IsUnique = true)]
    public class SessionParticipant
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int SessionId { get; set; }

        public Session? Session { get; set; }

        public int PlayerId { get; set; }

        public Player? Player { get; set; }

        public int Score { get; set; }

        public DateTime JoinedAt { get; set; }
    }
}
