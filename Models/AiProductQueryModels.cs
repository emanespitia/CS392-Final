namespace CS392_Final.Models;

public class AiProductQueryRequest
{
    public string Question { get; set; } = string.Empty;
}

public class AiProductQueryResponse
{
    public string Question { get; set; } = string.Empty;
    public string AiAnswer { get; set; } = string.Empty;
    public ProductQueryCriteria CriteriaUsed { get; set; } = new();
    public List<Product> Products { get; set; } = new();
}
