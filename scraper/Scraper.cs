using System.Net;

namespace scraper;

public class Scraper
{
	private readonly ILogger<Scraper> logger;
	private readonly ITvMazeClient mazeClient;

	public int MaxShow { get; private set; } = 0;
	public int MaxPage { get; private set; } = 0;


	public Scraper(ILogger<Scraper> logger, ITvMazeClient mazeClient)
	{
		this.logger = logger;
		this.mazeClient = mazeClient;
	}

	public async Task scrape(CancellationToken stoppingToken)
	{
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
			}
		}
		// commit mongo document
		MaxPage = Math.Max(MaxPage, page);
		MaxShow = Math.Max(MaxShow, showInfo.id);
	}
}