using MongoDB.Bson;
using MongoDB.Driver;

namespace api;

public interface IShowCastReadRepository
{
}


public class ShowCastReadRepository : IShowCastReadRepository
{
	private MongoClient mongoClient;
	private IMongoDatabase mongoDatabase;

	public ShowCastReadRepository(IConfiguration configuration)
	{
        string connectionString = configuration.GetValue<string>("connectionstrings:mongodb");
        mongoClient = new MongoClient(connectionString);
        mongoDatabase = mongoClient.GetDatabase("Scraper");
	}

	public Task<List<object>> getShowCasts()
	{
		var showCastCollection = mongoDatabase.GetCollection<BsonDocument>("ShowCast");
		var documents = showCastCollection.Find(new BsonDocument()).ToList();
		return Task.FromResult(documents.ConvertAll(BsonTypeMapper.MapToDotNetValue));
	}
}
