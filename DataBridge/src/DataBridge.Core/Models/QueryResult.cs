namespace DataBridge.Core.Models;

public class QueryResult<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int From { get; init; }
    public int Offset { get; init; }

    public bool HasNextPage => From + Offset < TotalCount;
    public bool HasPreviousPage => From > 0;
}
