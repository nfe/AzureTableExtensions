using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace FisTech.Persistence.AzureTable;

// TODO: Remove Azure.Data.Tables dependency and implement own OData json serializer

public class AzureTableRepository<TItem> : AzureTableReadOnlyRepository<TItem>, IAzureTableRepository<TItem>
    where TItem : class, new()
{
    public AzureTableRepository(IOptions<AzureTableRepositoryOptions> options, IAzureTableAdapter<TItem> adapter) :
        base(options, adapter) { }

    public Task AddAsync(TItem item, CancellationToken cancellationToken)
    {
        ITableEntity entity = Adapter.Adapt(item);
        return Client.AddEntityAsync(entity, cancellationToken);
    }

    public Task SaveAsync(TItem item, CancellationToken cancellationToken)
    {
        ITableEntity entity = Adapter.Adapt(item);
        return Client.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace, cancellationToken);
    }

    public Task RemoveAsync(TItem item, CancellationToken cancellationToken)
    {
        ITableEntity entity = Adapter.Adapt(item);
        return Client.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, entity.ETag, cancellationToken);
    }
}

public class AzureTableReadOnlyRepository<TItem> : IAzureTableReadOnlyRepository<TItem> where TItem : class, new()
{
    internal readonly AzureTableRepositoryOptions Options;
    internal readonly TableClient Client;
    internal readonly IAzureTableAdapter<TItem> Adapter;

    public AzureTableReadOnlyRepository(IOptions<AzureTableRepositoryOptions> options,
        IAzureTableAdapter<TItem> adapter)
    {
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        Client = new TableClient(Options.ConnectionString, Options.TableName);
        Adapter = adapter;
    }

    public async IAsyncEnumerable<TItem> AllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        AsyncPageable<TableEntity>? entities = Client.QueryAsync<TableEntity>(cancellationToken: cancellationToken);

        await foreach (TableEntity entity in entities)
            yield return Adapter.Adapt(entity);
    }

    public async Task<TItem?> OneAsync(string partitionKey, string rowKey, CancellationToken cancellationToken)
    {
        NullableResponse<TableEntity>? response =
            await Client.GetEntityIfExistsAsync<TableEntity>(partitionKey, rowKey,
                cancellationToken: cancellationToken);

        return response.HasValue ? Adapter.Adapt(response.Value) : null;
    }
}

public class AzureTableRepositoryOptions
{
    public AzureTableRepositoryOptions(string connectionString, string tableName)
    {
        ConnectionString = connectionString;
        TableName = tableName;
    }

    public string ConnectionString { get; set; }

    public string TableName { get; set; }
}