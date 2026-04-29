using CS392_Final.Models;

namespace CS392_Final.Services;

public interface IProductAiService
{
    Task<AiProductQueryResponse> AskProductsAsync(string question);
}
