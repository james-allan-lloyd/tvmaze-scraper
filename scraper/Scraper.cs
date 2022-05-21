using System.Net;

public class Scraper : BackgroundService
{
	private readonly ILogger<Scraper> logger;
	private readonly HttpClient client;
	private int maxShow = 0;
	private int maxPage = 0;

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
			logger.LogInformation("Scraping done (max page: {maxPage}, show: {maxShow})", maxPage, maxShow);
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


	async Task<T?> requestJsonWithBackoff<T>(string path, CancellationToken stoppingToken)
	{
		int backoffMs = 5000;
		while (!stoppingToken.IsCancellationRequested)
		{
			var url = "https://api.tvmaze.com" + path;
			logger.LogInformation("Requesting {url}", url);
			HttpResponseMessage response = await client.GetAsync(url);
			switch (response.StatusCode)
			{
				case HttpStatusCode.NotFound: return default(T);
				case HttpStatusCode.TooManyRequests:
					logger.LogWarning("Rate limit hit; backing off for {backoff}ms", backoffMs);
					await Task.Delay(backoffMs);
					break;
				case HttpStatusCode.OK:
					return await response.Content.ReadFromJsonAsync<T>();
				default:
					logger.LogWarning("Unexpected status recieved: {statusCode}, trying again...", response.StatusCode);
					await Task.Delay(backoffMs);
					break;
			}
		}
		return default(T);
	}


	async Task scrape(CancellationToken stoppingToken)
	{
		int page = 0;
		bool end = false;
		while (!end)
		{
			List<ShowInfo>? showInfoPage = await requestJsonWithBackoff<List<ShowInfo>>($"/shows?page={page}", stoppingToken);
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
	}


	private class PersonInfo
	{
		public int id { get; set; }
		public string? name { get; set; }
		public string? birthday { get; set; }
	}

	private class CastInfo
	{
		public PersonInfo? person { get; set;}
	}

	private async Task processShowsPage(int page, ShowInfo showInfo, CancellationToken stoppingToken)
	{
		List<CastInfo>? castInfoList = await requestJsonWithBackoff<List<CastInfo>>($"/shows/{showInfo.id}/cast", stoppingToken);
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
			}
		}
		// commit mongo document
		maxPage = Math.Max(maxPage, page);
		maxShow = Math.Max(maxShow, showInfo.id);
	}
}