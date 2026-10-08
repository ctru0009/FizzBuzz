using backend.DTOs;
using backend.Gameplay;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Tests.Integration;

[Trait("Category", "Integration")]
[Collection(IntegrationCollection.Name)]
public sealed class TickerTests(IntegrationTestFactory factory) : IntegrationTestBase(factory)
{
    private readonly IntegrationTestFactory _factory = factory;

    [Fact]
    public async Task TickAsync_AdvancesRoundAndBroadcastsNumberAdvanced()
    {
        var auth = await RegisterPlayerAsync($"tick{Guid.NewGuid():N}");
        var (_, session) = await CreateGameAndSessionAsync(auth, startRange: 1, endRange: 100);
        await using var hub = CreateHubConnection(auth);
        var broadcast = new TaskCompletionSource<NumberAdvancedPayload>(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.On<NumberAdvancedPayload>("NumberAdvanced", payload => broadcast.TrySetResult(payload));
        await hub.StartAsync();
        await hub.InvokeAsync<JoinSessionResult>("JoinSession", session.Id);

        using (var scope = _factory.Services.CreateScope())
        {
            var ticker = scope.ServiceProvider.GetRequiredService<ISessionTicker>();
            await ticker.TickAsync();
        }

        var completed = await Task.WhenAny(broadcast.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.Same(broadcast.Task, completed);
        var payload = await broadcast.Task;
        Assert.Equal(2, payload.Round);
        Assert.True(payload.AdvancesAtUtc > DateTime.UtcNow);

        using var snapshot = await auth.Client.GetAsync($"/api/sessions/{session.Id}");
        snapshot.EnsureSuccessStatusCode();
        var state = await ReadJsonAsync<SessionSnapshot>(snapshot);
        Assert.Equal(2, state.CurrentRound);
        Assert.Equal(payload.Number, state.CurrentNumber);
    }
}
