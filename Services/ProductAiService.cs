using System.Text;
using System.Text.Json;
using CS392_Final.Models;
using Microsoft.Extensions.Options;

namespace CS392_Final.Services;

public class ProductAiService : IProductAiService
{
    private readonly HttpClient _httpClient;
    private readonly IProductService _productService;
    private readonly GeminiSettings _geminiSettings;

    public ProductAiService(
        HttpClient httpClient,
        IProductService productService,
        IOptions<GeminiSettings> geminiSettings)
    {
        _httpClient = httpClient;
        _productService = productService;
        _geminiSettings = geminiSettings.Value;
    }

    public async Task<AiProductQueryResponse> AskProductsAsync(string question)
    {
        var criteria = await BuildCriteriaAsync(question);
        var products = await _productService.QueryAsync(criteria);
        var answer = await BuildNaturalLanguageAnswerAsync(question, criteria, products);

        return new AiProductQueryResponse
        {
            Question = question,
            AiAnswer = answer,
            CriteriaUsed = criteria,
            Products = products
        };
    }

    private async Task<ProductQueryCriteria> BuildCriteriaAsync(string question)
    {
        var prompt = """
            You convert user product questions into JSON query criteria.
            Return ONLY valid JSON with this schema:
            {
              "category": "string or null",
              "maxPrice": number or null,
              "minPrice": number or null,
              "inStock": true/false/null,
              "nameContains": "string or null",
              "tagsAny": ["string"],
              "sortBy": "price|name|null",
              "sortDirection": "asc|desc|null",
              "limit": number
            }
            Rules:
            - If user asks "cheapest", use sortBy=price and sortDirection=asc.
            - If user asks "most expensive", use sortBy=price and sortDirection=desc.
            - Default limit is 10.
            - If user asks for all, set limit to 50.
            User question:
            """ + question;

        var responseText = await CallGeminiAsync(prompt);
        var json = ExtractJson(responseText);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new ProductQueryCriteria { Limit = 10 };
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<ProductQueryCriteria>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return parsed ?? new ProductQueryCriteria { Limit = 10 };
        }
        catch
        {
            return new ProductQueryCriteria { Limit = 10 };
        }
    }

    private async Task<string> BuildNaturalLanguageAnswerAsync(
        string question,
        ProductQueryCriteria criteria,
        List<Product> products)
    {
        var productsJson = JsonSerializer.Serialize(products);
        var criteriaJson = JsonSerializer.Serialize(criteria);

        var prompt = """
            You are a shopping assistant. Answer naturally using only the provided product data.
            Keep it short and clear. If no products were found, say that and suggest adjusting filters.
            User question:
            """ + question + """

            Criteria used:
            """ + criteriaJson + """

            Product results:
            """ + productsJson;

        var responseText = await CallGeminiAsync(prompt);
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return products.Count == 0
                ? "I could not find matching products. Try broadening your search."
                : $"I found {products.Count} matching product(s).";
        }

        return responseText.Trim();
    }

    private async Task<string> CallGeminiAsync(string prompt)
    {
        var uri = $"https://generativelanguage.googleapis.com/v1beta/models/{_geminiSettings.Model}:generateContent?key={_geminiSettings.ApiKey}";

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.PostAsync(uri, content);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        var candidate = candidates[0];
        if (!candidate.TryGetProperty("content", out var contentNode))
        {
            return string.Empty;
        }

        if (!contentNode.TryGetProperty("parts", out var parts) || parts.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        return parts[0].GetProperty("text").GetString() ?? string.Empty;
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return string.Empty;
        }

        return text[start..(end + 1)];
    }
}
