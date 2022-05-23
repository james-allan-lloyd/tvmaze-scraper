using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace scraper;

public interface IShowCastRepository
{
	Task upsert(ShowInfo showInfo);
	Task completePage(int page);
	public int lastPageCompleted();
}


public class ShowCastRepository : IShowCastRepository
{
	class ProcessInfo {
		[BsonId]
		public int id { get; } = 0;
		public int lastPageCompleted { get; set; } = -1;
	}

	private ProcessInfo processInfo = new();

	private MongoClient mongoClient;
	private IMongoDatabase mongoDatabase;
	private IMongoCollection<ShowInfo> showCastCollection;
	private IMongoCollection<ProcessInfo> processCollection;

	public ShowCastRepository(IConfiguration configuration)
	{
        string connectionString = configuration.GetValue<string>("connectionstrings:mongodb");
        mongoClient = new MongoClient(connectionString);
        mongoDatabase = mongoClient.GetDatabase("Scraper");
		showCastCollection = mongoDatabase.GetCollection<ShowInfo>("ShowCast");
		processCollection = mongoDatabase.GetCollection<ProcessInfo>("Process");

		processInfo = processCollection.FindSync<ProcessInfo>(c => c.id == 0).FirstOrDefault() ?? processInfo;
	}

	public int lastPageCompleted() => processInfo.lastPageCompleted;

	public async Task completePage(int page)
	{
		processInfo.lastPageCompleted = page;
		await processCollection.ReplaceOneAsync(c => c.id == 0, processInfo, new ReplaceOptions { IsUpsert = true });
	}

	public async Task upsert(ShowInfo showInfo)
	{
		await showCastCollection.ReplaceOneAsync(c => c.id == showInfo.id, showInfo, new ReplaceOptions { IsUpsert = true });
	}
}
