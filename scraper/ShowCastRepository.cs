using MongoDB.Driver;

namespace scraper;

public interface IShowCastRepository
{
	Task upsert(ShowInfo showInfo);
}


public class ShowCastRepository : IShowCastRepository
{
	private MongoClient mongoClient;
	private IMongoDatabase mongoDatabase;

	public ShowCastRepository(IConfiguration configuration)
	{
        string connectionString = configuration.GetValue<string>("connectionstrings:mongodb");
        mongoClient = new MongoClient(connectionString);
        mongoDatabase = mongoClient.GetDatabase("Scraper");
	}

	public async Task upsert(ShowInfo showInfo)
	{
		var showCastCollection = mongoDatabase.GetCollection<ShowInfo>("ShowCast");
		await showCastCollection.ReplaceOneAsync(c => c.id == showInfo.id, showInfo, new ReplaceOptions { IsUpsert = true });
	}
}
