using System.ComponentModel.DataAnnotations;

namespace backend.DTOs
{
    public class GameRequestDTO : IValidatableObject
    {
        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        [Range(1, 10000)]
        public int StartRange { get; set; }

        [Range(1, 10000)]
        public int EndRange { get; set; }

        [Required]
        [MinLength(1)]
        [MaxLength(10)]
        public required RuleDTO[] Rules { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartRange >= EndRange)
            {
                yield return new ValidationResult(
                    "StartRange must be less than EndRange.",
                    [nameof(StartRange), nameof(EndRange)]);
            }
        }
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
}
