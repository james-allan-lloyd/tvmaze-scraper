namespace scraper;

public class ScraperService : BackgroundService
{
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
	}
}