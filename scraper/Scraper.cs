namespace scraper;

public class Scraper
{
	private readonly ILogger<Scraper> logger;
	private readonly ITvMazeClient mazeClient;
	private readonly IShowCastRepository showCastRepository;
	private readonly int? maxPage = default;


	public Scraper(ILogger<Scraper> logger, ITvMazeClient mazeClient, IShowCastRepository showCastRepository)
	{
		this.logger = logger;
		this.mazeClient = mazeClient;
		this.showCastRepository = showCastRepository;
		this.maxPage = 5;
	}


	// TODO: make endpoint a template that we subst page into
	public async Task scrapePages<T>(string endpoint, CancellationToken stoppingToken, Func<int, T, CancellationToken, Task> process)
	{
		var watch = System.Diagnostics.Stopwatch.StartNew();

		string name = endpoint;
		int page = await showCastRepository.lastPageCompleted(name) + 1;
		bool end = false;
		int entities = 0;
		int pages = 0;
		while (!end && (maxPage is null || page < maxPage))
		{
			logger.LogInformation("Started processing {name} page {page}", name, page);
			List<T>? pageData = await mazeClient.requestJsonWithBackoff<List<T>>(endpoint + "?page=" + page, stoppingToken);
			if(pageData != null)
			{
				foreach (var entity in pageData)
				{
					// TODO Batch processing here:
					await process(page, entity, stoppingToken);
					entities += 1;
				}
				await showCastRepository.completePage(name, page);
				pages += 1;
				logger.LogInformation("Completed processing {name} page {page}", name, page);
			}
			else
			{
				end = true;
				logger.LogInformation("Data for {name} ended at page {page}", name, page);
			}
			page += 1;
		}

		watch.Stop();
		logger.LogInformation("Scraping done in {elapsedMs}s: new pages {pages}, new entities {entities}", watch.ElapsedMilliseconds/1.0e3, pages, entities);
	}


	public static DateTime UnixTimeStampToDateTime(UInt64 unixTimeStamp )
	{
		// Unix timestamp is seconds past epoch
		DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		dateTime = dateTime.AddSeconds( unixTimeStamp ).ToLocalTime();
		return dateTime;
	}


	public async Task scrape(CancellationToken stoppingToken)
	{
		await scrapePages<ShowInfo>("/shows", stoppingToken, processShowInfo);
		// await scrapePages<PersonInfo>("/people", stoppingToken,
		// processPersonInfo);
	}

	public async Task update(CancellationToken stoppingToken)
	{
		logger.LogInformation("Starting updates...");
		var watch = System.Diagnostics.Stopwatch.StartNew();
		int updatesDone = 0;
		var showUpdates = await mazeClient.requestJsonWithBackoff<Dictionary<string, UInt64>>("/updates/shows?since=day", stoppingToken);
		if (showUpdates is null)
		{
			logger.LogWarning("Failed to get updates (null was returned)");
			return;
		}
		var lastMaxUpdate = await showCastRepository.lastMaxUpdate("/shows");
		foreach (var update in showUpdates!)
		{
			var showId = update.Key;
			if (update.Value > lastMaxUpdate)
			{
				var showInfo = await mazeClient.requestJsonWithBackoff<ShowInfo>($"/shows/{showId}", stoppingToken);
				if (showInfo is null)
				{
					logger.LogWarning("Failed to get show info for {showId}", showId);
				}
				else
				{
					await getShowCastInfo(showInfo, stoppingToken);
					await showCastRepository.upsert(showInfo);
					++updatesDone;
				}
			}
		}
		await showCastRepository.setLastUpdateTime("/shows", showUpdates.Values.Max());
		watch.Stop();
		logger.LogInformation("{count} updates done in {elapsedMs}s", updatesDone, watch.ElapsedMilliseconds/1.0e3);
	}


	private Task processPersonInfo(int page, PersonInfo personInfo, CancellationToken stoppingToken)
	{
		return Task.CompletedTask;
	}

	private async Task processShowInfo(int page, ShowInfo showInfo, CancellationToken stoppingToken)
	{
		await getShowCastInfo(showInfo, stoppingToken);
		showInfo.cast.Sort((x, y) => (y.birthday ?? "").CompareTo(x.birthday ?? ""));
		// commit mongo document
		await showCastRepository.upsert(showInfo);
	}

	private async Task getShowCastInfo(ShowInfo showInfo, CancellationToken stoppingToken)
	{
		// List<CastInfo>? castInfoList = await mazeClient.requestEmbedded<List<CastInfo>>($"/shows/{showInfo.id}?embed=cast", "cast", stoppingToken);
		List<CastInfo>? castInfoList = await mazeClient.requestJsonWithBackoff<List<CastInfo>>($"/shows/{showInfo.id}/cast", stoppingToken);
		if(castInfoList == null)
		{
			logger.LogWarning("Failed to get cast list for show id {showId}", showInfo.id);
			return;
		}
		foreach(var castInfo in castInfoList)
		{
			if(castInfo.person == null)
			{
				logger.LogWarning("Non person cast member for show {showId}", showInfo.id);
			}
			else
			{
				// add to mongo document
				showInfo.cast.Add(castInfo.person);
			}
		}
	}
}