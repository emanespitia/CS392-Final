using CS392_Final.Models;
using CS392_Final.Services;
using Microsoft.AspNetCore.Mvc;

namespace CS392_Final.Controllers;

[ApiController]
[Route("api/ai/products")]
public class ProductAiController : ControllerBase
{
    private readonly IProductAiService _productAiService;

    public ProductAiController(IProductAiService productAiService)
    {
        _productAiService = productAiService;
    }

    [HttpPost("ask")]
    public async Task<ActionResult<AiProductQueryResponse>> Ask(AiProductQueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest("Question is required.");
        }

        var result = await _productAiService.AskProductsAsync(request.Question);
        return Ok(result);
    }
}
