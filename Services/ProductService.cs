using CS392_Final.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CS392_Final.Services;

public class ProductService : IProductService
{
    private readonly IMongoCollection<Product> _productsCollection;

    public ProductService(IOptions<MongoDbSettings> mongoDbSettings)
    {
        var client = new MongoClient(mongoDbSettings.Value.ConnectionString);
        var database = client.GetDatabase(mongoDbSettings.Value.DatabaseName);
        _productsCollection = database.GetCollection<Product>(mongoDbSettings.Value.CollectionName);
    }

    public async Task<List<Product>> GetAllAsync() =>
        await _productsCollection.Find(_ => true).ToListAsync();

    public async Task<Product?> GetByIdAsync(string id) =>
        await _productsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task CreateAsync(Product product) =>
        await _productsCollection.InsertOneAsync(product);

    public async Task<bool> UpdateAsync(string id, Product updatedProduct)
    {
        updatedProduct.Id = id;
        var result = await _productsCollection.ReplaceOneAsync(x => x.Id == id, updatedProduct);
        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _productsCollection.DeleteOneAsync(x => x.Id == id);
        return result.DeletedCount > 0;
    }
}
