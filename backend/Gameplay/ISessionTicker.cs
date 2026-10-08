namespace backend.Gameplay
{
    public interface ISessionTicker
    {
        // Runs one pass over all open sessions: finishes expired ones, advances the rest.
        Task TickAsync(CancellationToken cancellationToken = default);
    }
}
