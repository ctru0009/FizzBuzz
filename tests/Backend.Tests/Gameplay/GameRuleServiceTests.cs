using backend.Models;

namespace backend.Tests.Gameplay;

public sealed class GameRuleServiceTests : IDisposable
{
    private readonly GameplayFixture _fx = new();

    [Theory]
    [InlineData(15, "FizzBuzz", true)]
    [InlineData(15, "fizzbuzz", true)]
    [InlineData(15, "  FIZZBUZZ  ", true)]
    [InlineData(15, "Fizz", false)]
    [InlineData(3, "Fizz", true)]
    [InlineData(5, "Buzz", true)]
    [InlineData(7, "7", true)]
    [InlineData(7, "Fizz", false)]
    [InlineData(7, "", false)]
    public void IsCorrectAnswer_MatchesContractRules(int number, string answer, bool expected)
    {
        var rules = new[]
        {
            new Rule { DivisibleBy = 3, ReplacementWord = "Fizz" },
            new Rule { DivisibleBy = 5, ReplacementWord = "Buzz" },
        };

        Assert.Equal(expected, _fx.Rules.IsCorrectAnswer(number, answer, rules));
    }

    [Fact]
    public void IsCorrectAnswer_SkipsDivisorsUnderOne()
    {
        var rules = new[]
        {
            new Rule { DivisibleBy = 0, ReplacementWord = "Zero" },
            new Rule { DivisibleBy = -2, ReplacementWord = "Neg" },
        };

        // 4 is divisible by -2, but negative and zero divisors are skipped,
        // so the expected answer is the number itself.
        Assert.True(_fx.Rules.IsCorrectAnswer(4, "4", rules));
        Assert.False(_fx.Rules.IsCorrectAnswer(4, "Neg", rules));
        Assert.False(_fx.Rules.IsCorrectAnswer(4, "Zero", rules));
        Assert.True(_fx.Rules.IsCorrectAnswer(7, "7", rules));
    }

    [Fact]
    public async Task ValidateAnswerAsync_LoadsRulesFromDatabase()
    {
        var player = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(player.Id, player.Name, 1, 100, (3, "Fizz"), (5, "Buzz"));

        Assert.True(await _fx.Rules.ValidateAnswerAsync(game.Id, 15, "FizzBuzz"));
        Assert.True(await _fx.Rules.ValidateAnswerAsync(game.Id, 7, "7"));
        Assert.False(await _fx.Rules.ValidateAnswerAsync(game.Id, 15, "Fizz"));
    }

    [Fact]
    public void PickNumber_StaysInRange()
    {
        for (var i = 0; i < 200; i++)
        {
            Assert.InRange(_fx.Rules.PickNumber(1, 100, []), 1, 100);
        }
    }

    [Fact]
    public void PickNumber_AvoidsUsedNumbersWhenPossible()
    {
        var used = Enumerable.Range(1, 90).ToList();
        for (var i = 0; i < 50; i++)
        {
            Assert.InRange(_fx.Rules.PickNumber(1, 100, used), 91, 100);
        }
    }

    [Fact]
    public void PickNumber_AllUsed_AllowsRepeatAndTerminates()
    {
        var picked = _fx.Rules.PickNumber(1, 2, [1, 2]);
        Assert.True(picked is 1 or 2);
    }

    [Fact]
    public void PickNumber_SingleValueRange_ReturnsIt()
    {
        Assert.Equal(5, _fx.Rules.PickNumber(5, 5, []));
        Assert.Equal(5, _fx.Rules.PickNumber(5, 5, [5]));
    }

    [Fact]
    public void PickNumber_IncludesEndRange()
    {
        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(2, _fx.Rules.PickNumber(1, 2, [1]));
        }
    }

    public void Dispose() => _fx.Dispose();
}
