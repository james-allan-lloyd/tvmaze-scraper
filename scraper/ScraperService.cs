namespace scraper;

public class ScraperService : BackgroundService
{
	private const int POLLING_INTERVAL_MS = 60_000;
	private readonly ILogger<ScraperService> logger;
	private readonly Scraper scraper;

	public ScraperService(ILogger<ScraperService> logger, Scraper scraper)
	{
		this.logger = logger;
		this.scraper = scraper;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		logger.LogInformation("Starting scraping");
		try
		{
			await scraper.scrape(stoppingToken);
		}
		catch(Exception e)
		{
			logger.LogError(e, "Scraper exited with exception: {what}", e.Message);
		}

		// var updateInterval = TimeSpan.FromHours(1);
		var updateInterval = TimeSpan.FromSeconds(1);
		while (!stoppingToken.IsCancellationRequested)
		{
			DateTime nextUpdate = DateTime.UtcNow + updateInterval;
			logger.LogInformation("Next update at {nextUpdate}", nextUpdate.ToLocalTime().ToShortTimeString());
			while (DateTime.UtcNow < nextUpdate)
			{
				await Task.Delay(POLLING_INTERVAL_MS); // some light polling at minute intervals
			}
			while (nextUpdate <= DateTime.UtcNow)
			{
				// in case we took longer than the update interval (in testing)
				nextUpdate += updateInterval;
			}
			await scraper.update(stoppingToken);
		}
	}
}