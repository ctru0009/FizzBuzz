using backend.Models;

namespace backend.Gameplay
{
    public interface IGameRuleService
    {
        bool IsCorrectAnswer(int number, string? answer, IEnumerable<Rule> rules);

        Task<bool> ValidateAnswerAsync(int gameId, int number, string? answer, CancellationToken cancellationToken = default);

        int PickNumber(int startRange, int endRange, IReadOnlyCollection<int> usedNumbers);
    }
}
