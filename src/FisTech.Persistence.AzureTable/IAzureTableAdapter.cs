using Azure.Data.Tables;

namespace FisTech.Persistence.AzureTable;

public interface IAzureTableAdapter<TSource> where TSource : class, new()
{
    ITableEntity Adapt(TSource item);

    TSource Adapt(TableEntity entity);
}