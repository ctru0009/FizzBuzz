using backend.DTOs;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Gameplay;

public sealed class GameSessionStoreTests : IDisposable
{
    private readonly GameplayFixture _fx = new();

    [Fact]
    public async Task CreateSession_SeedsNumberRoundOneAndAutoJoinsCreator()
    {
        var player = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(player.Id, player.Name, 1, 100, (3, "Fizz"));

        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, player.Id, 300);

        Assert.Equal(game.Id, snapshot.GameId);
        Assert.Equal(SessionStatus.Open, snapshot.Status);
        Assert.Equal(1, snapshot.CurrentRound);
        Assert.InRange(snapshot.CurrentNumber, 1, 100);
        Assert.True(snapshot.EndTimeUtc > snapshot.StartTimeUtc);
        var score = Assert.Single(snapshot.Scores);
        Assert.Equal(player.Id, score.PlayerId);
        Assert.Equal("Ann", score.PlayerName);
        Assert.Equal(0, score.Score);

        var used = await _fx.Redis.GetUsedNumbersAsync(snapshot.Id);
        Assert.Equal([snapshot.CurrentNumber], used);
    }

    [Fact]
    public async Task CreateSession_MissingGame_ThrowsNotFound()
    {
        var player = await _fx.AddPlayerAsync("Ann");

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _fx.Store.CreateSessionAsync(999, player.Id, 300));
        Assert.Equal("Game not found.", ex.Message);
    }

    [Fact]
    public async Task JoinSession_SecondPlayer_ScoreZeroOthersKept()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var bob = await _fx.AddPlayerAsync("Bob");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);
        await _fx.Store.RecordAnswerAsync(snapshot.Id, ann.Id, 1, true);

        var joined = await _fx.Store.JoinSessionAsync(snapshot.Id, bob.Id);

        Assert.Equal(2, joined.Snapshot.Scores.Length);
        Assert.Equal(10, joined.Snapshot.Scores.First(s => s.PlayerId == ann.Id).Score);
        Assert.Equal(0, joined.Snapshot.Scores.First(s => s.PlayerId == bob.Id).Score);
        Assert.False(joined.AnsweredCurrentRound);
    }

    [Fact]
    public async Task JoinSession_Rejoin_ResumesScore()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);
        await _fx.Store.RecordAnswerAsync(snapshot.Id, ann.Id, 1, true);

        var rejoined = await _fx.Store.JoinSessionAsync(snapshot.Id, ann.Id);

        Assert.Equal(10, rejoined.Snapshot.Scores.Single().Score);
        Assert.True(rejoined.AnsweredCurrentRound);
        Assert.Equal(1, await _fx.Context.SessionParticipants.CountAsync(p => p.SessionId == snapshot.Id && p.PlayerId == ann.Id));
    }

    [Fact]
    public async Task JoinSession_Missing_ThrowsNotFound()
    {
        var ann = await _fx.AddPlayerAsync("Ann");

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _fx.Store.JoinSessionAsync(999, ann.Id));
        Assert.Equal("Session not found.", ex.Message);
    }

    [Fact]
    public async Task JoinSession_Finished_Throws()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var bob = await _fx.AddPlayerAsync("Bob");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);
        await _fx.Store.FinishSessionAsync(snapshot.Id);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _fx.Store.JoinSessionAsync(snapshot.Id, bob.Id));
        Assert.Equal("Session has finished.", ex.Message);
    }

    [Fact]
    public async Task RecordAnswer_Correct_AddsTen()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);

        var score = await _fx.Store.RecordAnswerAsync(snapshot.Id, ann.Id, 1, true);

        Assert.Equal(10, score);
        var latest = await _fx.Store.GetSnapshotAsync(snapshot.Id);
        Assert.Equal(10, latest!.Scores.Single().Score);
    }

    [Fact]
    public async Task RecordAnswer_Wrong_AddsZero()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);

        var score = await _fx.Store.RecordAnswerAsync(snapshot.Id, ann.Id, 1, false);

        Assert.Equal(0, score);
    }

    [Fact]
    public async Task RecordAnswer_DuplicateRound_Rejected()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);
        await _fx.Store.RecordAnswerAsync(snapshot.Id, ann.Id, 1, true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _fx.Store.RecordAnswerAsync(snapshot.Id, ann.Id, 1, true));
        Assert.Equal("Already answered this round.", ex.Message);
        var latest = await _fx.Store.GetSnapshotAsync(snapshot.Id);
        Assert.Equal(10, latest!.Scores.Single().Score);
    }

    [Fact]
    public async Task RecordAnswer_NotJoined_Rejected()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var bob = await _fx.AddPlayerAsync("Bob");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _fx.Store.RecordAnswerAsync(snapshot.Id, bob.Id, 1, true));
        Assert.Equal("Join the session before answering.", ex.Message);
    }

    [Fact]
    public async Task RecordAnswer_Finished_Rejected()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);
        await _fx.Store.FinishSessionAsync(snapshot.Id);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _fx.Store.RecordAnswerAsync(snapshot.Id, ann.Id, 1, true));
        Assert.Equal("Session has finished.", ex.Message);
    }

    [Fact]
    public async Task ListOpenSessions_OnlyOpenOrdered()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var first = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);
        var second = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);
        await _fx.Store.FinishSessionAsync(first.Id);

        var open = await _fx.Store.ListOpenSessionsAsync();

        var info = Assert.Single(open);
        Assert.Equal(second.Id, info.Id);
        Assert.Equal(game.Id, info.GameId);
        Assert.Equal("Test game", info.GameName);
        Assert.Equal(1, info.PlayerCount);
        Assert.Equal(second.EndTimeUtc, info.EndsAtUtc);
    }

    [Fact]
    public async Task FinishSession_Idempotent_ReturnsFinalScores()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);
        await _fx.Store.RecordAnswerAsync(snapshot.Id, ann.Id, 1, true);

        var final = await _fx.Store.FinishSessionAsync(snapshot.Id);
        var again = await _fx.Store.FinishSessionAsync(snapshot.Id);

        Assert.Equal(10, final!.Single().Score);
        Assert.Equal(10, again!.Single().Score);
        var latest = await _fx.Store.GetSnapshotAsync(snapshot.Id);
        Assert.Equal(SessionStatus.Finished, latest!.Status);
    }

    [Fact]
    public async Task AdvanceSession_BumpsNumberAndRound()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);

        var advanced = await _fx.Store.AdvanceSessionAsync(snapshot.Id, 42);

        Assert.NotNull(advanced);
        Assert.Equal(42, advanced.Number);
        Assert.Equal(2, advanced.Round);
        var latest = await _fx.Store.GetSnapshotAsync(snapshot.Id);
        Assert.Equal(42, latest!.CurrentNumber);
        Assert.Equal(2, latest.CurrentRound);
    }

    [Fact]
    public async Task AdvanceSession_FinishedOrMissing_ReturnsNull()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);
        await _fx.Store.FinishSessionAsync(snapshot.Id);

        Assert.Null(await _fx.Store.AdvanceSessionAsync(snapshot.Id, 42));
        Assert.Null(await _fx.Store.AdvanceSessionAsync(999, 42));
    }

    public void Dispose() => _fx.Dispose();
}
