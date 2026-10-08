namespace backend.Interfaces.Cache
{
    public interface IRedisCachingService
    {
        // Per session used numbers at key session:{id}:used, JSON int list.
        Task<IReadOnlyList<int>?> GetUsedNumbersAsync(int sessionId, CancellationToken cancellationToken = default);

        Task AddUsedNumberAsync(int sessionId, int number, TimeSpan ttl, CancellationToken cancellationToken = default);
    }
}
