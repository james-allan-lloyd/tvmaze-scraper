using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace scraper;

public interface IShowCastRepository
{
	Task upsert(ShowInfo showInfo);
	Task completePage(string name, int page);
	Task<int> lastPageCompleted(string name);
	Task setLastUpdateTime(string name, ulong lastUpdateTime);
	Task<UInt64> lastMaxUpdate(string name);
}


public class ShowCastRepository : IShowCastRepository
{
	class ProcessInfo {
		public ProcessInfo(string name) => this.name = name;

		[BsonId]
		public string name { get; init; }
		public int lastPageCompleted { get; set; } = -1;
		public UInt64 lastUpdateTimestamp { get; set; } = 0;
	}

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
		ProcessInfo processInfo = await getProcessInfo(name);
		logger.LogInformation("last completed page for {name} is {page}", name, processInfo.lastPageCompleted);
		return processInfo.lastPageCompleted;
	}

	private async Task<ProcessInfo> getProcessInfo(string name)
	{
		return await processCollection.Find<ProcessInfo>(c => c.name == name).FirstOrDefaultAsync() ?? new ProcessInfo(name);
	}

	public async Task completePage(string name, int page)
	{
		ProcessInfo processInfo = await getProcessInfo(name);
		processInfo.lastPageCompleted = page;
		await processCollection.ReplaceOneAsync(c => c.name == name, processInfo, new ReplaceOptions { IsUpsert = true });
	}

	public async Task upsert(ShowInfo showInfo)
	{
		await showCastCollection.ReplaceOneAsync(c => c.id == showInfo.id, showInfo, new ReplaceOptions { IsUpsert = true });
	}

	public async Task setLastUpdateTime(string name, ulong lastUpdateTime)
	{
		ProcessInfo processInfo = await getProcessInfo(name);
		processInfo.lastUpdateTimestamp = lastUpdateTime;
		await processCollection.ReplaceOneAsync(c => c.name == name, processInfo, new ReplaceOptions { IsUpsert = true });
	}

	public async Task<UInt64> lastMaxUpdate(string name)
	{
		ProcessInfo processInfo = await getProcessInfo(name);
		logger.LogInformation("last completed page for {name} is {page}", name, processInfo.lastPageCompleted);
		return processInfo.lastUpdateTimestamp;
	}
}
