using MongoDB.Bson;
using MongoDB.Driver;

namespace api;

public interface IShowCastReadRepository
{
	public Task<List<object>> getShowCasts(int page, int pageSize);
}


public class ShowCastReadRepository : IShowCastReadRepository
{
	private readonly ILogger<ShowCastReadRepository> logger;
	private MongoClient mongoClient;
	private IMongoDatabase mongoDatabase;

	public ShowCastReadRepository(IConfiguration configuration, ILogger<ShowCastReadRepository> logger)
	{
        string connectionString = configuration.GetValue<string>("connectionstrings:mongodb");
        mongoClient = new MongoClient(connectionString);
        mongoDatabase = mongoClient.GetDatabase("Scraper");
		this.logger = logger;
	}

	public async Task<List<object>> getShowCasts(int page, int pageSize)
	{
		var showCastCollection = mongoDatabase.GetCollection<BsonDocument>("ShowCast");
		var documents = await showCastCollection.Find(new BsonDocument())
			.Skip((page - 1) * pageSize)
			.Limit(pageSize)
			.Project("{_id: 0, id: \"$_id\", name: \"$name\", cast: \"$cast\"}")
			.ToListAsync();
		logger.LogInformation("Returning {count} show casts, requested size {count}", documents.Count, pageSize);
		return documents.ConvertAll(BsonTypeMapper.MapToDotNetValue);
	}
}
