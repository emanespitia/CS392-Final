using CS392_Final.Models;

namespace CS392_Final.Services;

public interface IProductService
{
    Task<List<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(string id);
    Task<List<Product>> QueryAsync(ProductQueryCriteria criteria);
    Task CreateAsync(Product product);
    Task<bool> UpdateAsync(string id, Product updatedProduct);
    Task<bool> DeleteAsync(string id);
}
