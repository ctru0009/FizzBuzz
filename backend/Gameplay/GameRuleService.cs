using System.Text;
using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Gameplay
{
    public sealed class GameRuleService : IGameRuleService
    {
        private const int MaxPickerTries = 100;

        private readonly BackendAppDbContext _context;

        public GameRuleService(BackendAppDbContext context)
        {
            _context = context;
        }

        public bool IsCorrectAnswer(int number, string? answer, IEnumerable<Rule> rules)
        {
            var expected = new StringBuilder();
            foreach (var rule in rules)
            {
                if (rule.DivisibleBy < 1)
                {
                    continue;
                }

                if (number % rule.DivisibleBy == 0)
                {
                    expected.Append(rule.ReplacementWord);
                }
            }

            var trimmed = answer?.Trim();
            if (expected.Length == 0)
            {
                return string.Equals(trimmed, number.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(expected.ToString(), trimmed, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> ValidateAnswerAsync(int gameId, int number, string? answer, CancellationToken cancellationToken = default)
        {
            var rules = await _context.Rules
                .Where(r => r.GameId == gameId)
                .OrderBy(r => r.Id)
                .ToListAsync(cancellationToken);
            return IsCorrectAnswer(number, answer, rules);
        }

        public int PickNumber(int startRange, int endRange, IReadOnlyCollection<int> usedNumbers)
        {
            if (startRange > endRange)
            {
                throw new ArgumentOutOfRangeException(nameof(startRange), "StartRange must be less than or equal to EndRange.");
            }

            if (startRange == endRange)
            {
                return startRange;
            }

            var exclusiveEnd = endRange == int.MaxValue ? endRange : endRange + 1;
            var candidate = startRange;
            for (var i = 0; i < MaxPickerTries; i++)
            {
                candidate = Random.Shared.Next(startRange, exclusiveEnd);
                if (!usedNumbers.Contains(candidate))
                {
                    return candidate;
                }
            }

            return candidate;
        }
    }
}
