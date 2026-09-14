namespace WahlMirai.Web.ViewModels;

/// <summary>
/// Contenedor genérico para resultados paginados server-side.
/// </summary>
/// <typeparam name="T">Tipo del elemento en la página.</typeparam>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public int StartItemIndex => TotalCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;
    public int EndItemIndex => Math.Min(PageNumber * PageSize, TotalCount);
}
