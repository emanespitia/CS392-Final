using CS392_Final.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

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

    public async Task<List<Product>> QueryAsync(ProductQueryCriteria criteria)
    {
        var filterBuilder = Builders<Product>.Filter;
        var filters = new List<FilterDefinition<Product>>();

        if (!string.IsNullOrWhiteSpace(criteria.Category))
        {
            filters.Add(filterBuilder.Eq(x => x.Category, criteria.Category));
        }

        if (criteria.MinPrice.HasValue)
        {
            filters.Add(filterBuilder.Gte(x => x.Price, criteria.MinPrice.Value));
        }

        if (criteria.MaxPrice.HasValue)
        {
            filters.Add(filterBuilder.Lte(x => x.Price, criteria.MaxPrice.Value));
        }

        if (criteria.InStock.HasValue)
        {
            filters.Add(filterBuilder.Eq(x => x.InStock, criteria.InStock.Value));
        }

        if (!string.IsNullOrWhiteSpace(criteria.NameContains))
        {
            filters.Add(filterBuilder.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(criteria.NameContains, "i")));
        }

        if (criteria.TagsAny is { Count: > 0 })
        {
            filters.Add(filterBuilder.AnyIn(x => x.Tags, criteria.TagsAny));
        }

        var combinedFilter = filters.Count > 0 ? filterBuilder.And(filters) : filterBuilder.Empty;
        var findQuery = _productsCollection.Find(combinedFilter);

        if (string.Equals(criteria.SortBy, "price", StringComparison.OrdinalIgnoreCase))
        {
            findQuery = string.Equals(criteria.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                ? findQuery.SortByDescending(x => x.Price)
                : findQuery.SortBy(x => x.Price);
        }
        else if (string.Equals(criteria.SortBy, "name", StringComparison.OrdinalIgnoreCase))
        {
            findQuery = string.Equals(criteria.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                ? findQuery.SortByDescending(x => x.Name)
                : findQuery.SortBy(x => x.Name);
        }

        var limit = criteria.Limit.GetValueOrDefault(10);
        limit = Math.Clamp(limit, 1, 50);
        findQuery = findQuery.Limit(limit);

        return await findQuery.ToListAsync();
    }

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
