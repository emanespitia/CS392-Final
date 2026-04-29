namespace CS392_Final.Models;

public class ProductQueryCriteria
{
    public string? Category { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? MinPrice { get; set; }
    public bool? InStock { get; set; }
    public string? NameContains { get; set; }
    public List<string>? TagsAny { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
    public int? Limit { get; set; }
}
