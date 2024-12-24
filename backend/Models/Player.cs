using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Index(nameof(Name), IsUnique = true)]
    public class Player
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalScores { get; set; }
        public int TotalGamesPlayed { get; set; }
        public ICollection<Session>? Sessions { get; set; } = [];

        public ICollection<Game>? Games { get; set; } = [];

    }
}
