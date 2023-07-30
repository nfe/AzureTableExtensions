namespace FisTech.Persistence.AzureTable;

public interface IAzureTableRepository<TItem> : IAzureTableReadOnlyRepository<TItem> where TItem : class, new()
{
    Task AddAsync(TItem item, CancellationToken cancellationToken = default);

    Task SaveAsync(TItem item, CancellationToken cancellationToken = default);

    Task RemoveAsync(TItem item, CancellationToken cancellationToken = default);
}

public interface IAzureTableReadOnlyRepository<TItem> where TItem : class, new()
{
    IAsyncEnumerable<TItem> AllAsync(CancellationToken cancellationToken = default);

    Task<TItem?> OneAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default);
}