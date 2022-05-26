using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace scraper;

public interface IShowCastRepository
{
	Task upsert(ShowInfo showInfo);
	Task completePage(string name, int page);
	public Task<int> lastPageCompleted(string name);
}


public class ShowCastRepository : IShowCastRepository
{
	class ProcessInfo {
		[BsonId]
		public string name { get; set; } = "";
		public int lastPageCompleted { get; set; } = -1;
	}

	private ProcessInfo processInfo = new();

	private MongoClient mongoClient;
	private IMongoDatabase mongoDatabase;
	private IMongoCollection<ShowInfo> showCastCollection;
	private IMongoCollection<ProcessInfo> processCollection;
	private readonly ILogger<ShowCastRepository> logger;

	public ShowCastRepository(IConfiguration configuration, ILogger<ShowCastRepository> logger)
	{
		this.logger = logger;

        string connectionString = configuration.GetValue<string>("connectionstrings:mongodb");
        mongoClient = new MongoClient(connectionString);
        mongoDatabase = mongoClient.GetDatabase("Scraper");
		showCastCollection = mongoDatabase.GetCollection<ShowInfo>("ShowCast");
		processCollection = mongoDatabase.GetCollection<ProcessInfo>("Process");

	}

	public async Task<int> lastPageCompleted(string name) {
		processInfo = await processCollection.Find<ProcessInfo>(c => c.name == name).FirstOrDefaultAsync();
		if (processInfo is null)
		{
			logger.LogInformation("start of processing for {name}", name);
			return -1;
		}
		else
		{
			logger.LogInformation("last completed page for {name} is {page}", name, processInfo.lastPageCompleted);
			return processInfo.lastPageCompleted;
		}
	}

	public async Task completePage(string name, int page)
	{
		await processCollection.ReplaceOneAsync(c => c.name == name, new ProcessInfo { name = name, lastPageCompleted = page}, new ReplaceOptions { IsUpsert = true });
	}

	public async Task upsert(ShowInfo showInfo)
	{
		await showCastCollection.ReplaceOneAsync(c => c.id == showInfo.id, showInfo, new ReplaceOptions { IsUpsert = true });
	}
}
