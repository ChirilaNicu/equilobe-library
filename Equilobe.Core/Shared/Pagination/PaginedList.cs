namespace Equilobe.Core.Shared.Pagination;

public class PaginatedList<T>
{
    public List<T> Items { get; }
    public int TotalItems { get; }
    public int PageNumber { get; }
    public int PageSize { get; }

    public PaginatedList(List<T> items, int totalItems, int pageNumber, int pageSize)
    {
        Items = items;
        TotalItems = totalItems;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
