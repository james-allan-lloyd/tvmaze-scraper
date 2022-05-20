using System.Net;

public class Scraper : BackgroundService
{
	private readonly ILogger<Scraper> logger;
	private readonly HttpClient client;

	public Scraper(ILogger<Scraper> logger)
	{
		this.logger = logger;
		this.client = new HttpClient();
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		logger.LogInformation("Starting scraping");
		try
		{
			await scrape(stoppingToken);
			logger.LogInformation("Scraping done.");
		}
		catch(Exception e)
		{
			logger.LogError(e, "Scraper exited with exception: {what}", e.Message);
		}
	}

	private class ShowInfo
	{
		public int id { get; set; }
		public string? name { get; set; }
	}

	async Task scrape(CancellationToken stoppingToken)
	{
		int page = 1;
		int backoffMs = 5000;
		bool end = false;
		while (!end)
		{
			HttpResponseMessage response = await client.GetAsync($"https://api.tvmaze.com/shows?page={page}");
			switch (response.StatusCode)
			{
				case HttpStatusCode.NotFound: end = true; break;
				case HttpStatusCode.TooManyRequests:
					logger.LogWarning("Rate limit hit; backing off for {backoff}ms", backoffMs);
					await Task.Delay(backoffMs);
					break;
				case HttpStatusCode.OK:
					List<ShowInfo>? showInfo = await response.Content.ReadFromJsonAsync<List<ShowInfo>>();
					if(showInfo != null)
						showInfo.ForEach(s => processShowsPage(page, s));
					page += 1;
					end = true;
					break;
				default:
					logger.LogWarning("Unexpected status recieved: {statusCode}", response.StatusCode);
					break;
			}

		}
	}

	private void processShowsPage(int page, ShowInfo showInfo)
	{
	}
}