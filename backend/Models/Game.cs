using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Game
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string Name { get; set; }

        public int PlayerId { get; set; }

        public required string AuthorName { get; set; }

        public int StartRange { get; set; }

        public int EndRange { get; set; }

        public DateTime CreatedAt { get; set; }

        public ICollection<Rule>? Rules { get; set; } = [];

        public ICollection<Session>? Sessions { get; set; } = [];

        public Player? Player { get; set; }
    }
}
