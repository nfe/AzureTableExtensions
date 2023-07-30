using Azure.Data.Tables;

namespace FisTech.Persistence.AzureTable;

public interface IAzureTableAdapter<TItem> where TItem : class, new()
{
    ITableEntity Adapt(TItem item);

    TItem Adapt(TableEntity entity);
}