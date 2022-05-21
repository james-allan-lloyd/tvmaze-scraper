namespace scraper;

public class Scraper
{
	private readonly ILogger<Scraper> logger;
	private readonly ITvMazeClient mazeClient;
	private readonly IShowCastRepository showCastRepository;

	public int MaxShow { get; private set; } = 0;
	public int MaxPage { get; private set; } = 0;

	public Scraper(ILogger<Scraper> logger, ITvMazeClient mazeClient, IShowCastRepository showCastRepository)
	{
		this.logger = logger;
		this.mazeClient = mazeClient;
		this.showCastRepository = showCastRepository;
	}

	public async Task scrape(CancellationToken stoppingToken)
	{
		var watch = System.Diagnostics.Stopwatch.StartNew();
		int page = 0;
		bool end = false;
		while (!end)
		{
			List<ShowInfo>? showInfoPage = await mazeClient.requestJsonWithBackoff<List<ShowInfo>>($"/shows?page={page}", stoppingToken);
			if(showInfoPage != null)
			{
				foreach (var showInfo in showInfoPage)
				{
					// TODO Batch processing here:
					await processShowsPage(page, showInfo, stoppingToken);
				}
			}
			page += 1;
			end = true;
		}
		// the code that you want to measure comes here
		watch.Stop();
		logger.LogInformation("Scraping done in {elapsedMs}s", watch.ElapsedMilliseconds/1.0e3);
	}

	private async Task processShowsPage(int page, ShowInfo showInfo, CancellationToken stoppingToken)
	{
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
		// commit mongo document
		await showCastRepository.upsert(showInfo);
		MaxPage = Math.Max(MaxPage, page);
		MaxShow = Math.Max(MaxShow, showInfo.id);
	}
}