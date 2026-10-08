using System.ComponentModel.DataAnnotations;

namespace backend.DTOs
{
    public class RuleDTO
    {
        [Range(1, int.MaxValue)]
        public int DivisibleBy { get; set; }

        [Required]
        [MaxLength(50)]
        public required string ReplacementWord { get; set; }
    }
}
