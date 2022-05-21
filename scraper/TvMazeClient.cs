using System.Net;

namespace scraper;

public interface ITvMazeClient
{
	Task<T?> requestJsonWithBackoff<T>(string path, CancellationToken stoppingToken);
}

public class TvMazeClient : ITvMazeClient
{
	private readonly ILogger<TvMazeClient> logger;
	private readonly HttpClient client;
	public TvMazeClient(ILogger<TvMazeClient> logger)
	{
		this.logger = logger;
		this.client = new HttpClient();
	}

	public async Task<T?> requestJsonWithBackoff<T>(string path, CancellationToken stoppingToken)
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
}