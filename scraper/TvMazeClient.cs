using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace scraper;

public interface ITvMazeClient
{
	Task<T?> requestJsonWithBackoff<T>(string path, CancellationToken stoppingToken);
	Task<T?> requestEmbedded<T>(string path, string embeddedType, CancellationToken stoppingToken);
}

public class TvMazeClient : ITvMazeClient
{
	private readonly ILogger<TvMazeClient> logger;
	private readonly HttpClient client;
	private int requestsSinceLastBackoff;
	private DateTime lastBackoff;

	public TvMazeClient(ILogger<TvMazeClient> logger)
	{
		this.logger = logger;
		this.client = new HttpClient();
		this.requestsSinceLastBackoff = 0;
		this.lastBackoff = DateTime.UtcNow;
	}


	public async Task<HttpResponseMessage?> requestWithBackoff(string path, CancellationToken stoppingToken)
	{
		var url = "https://api.tvmaze.com" + path;
		const int backoffMs = 10000;
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				HttpResponseMessage response = await client.GetAsync(url);
				switch (response.StatusCode)
				{
					case HttpStatusCode.NotFound:
						requestsSinceLastBackoff++;
						return null;
					case HttpStatusCode.OK:
						requestsSinceLastBackoff++;
						return response;

					case HttpStatusCode.TooManyRequests:
						var now = DateTime.UtcNow;
						var timeSinceLastBackoff = now - lastBackoff;
						logger.LogWarning("Rate limit hit after {requests} requests in {timeSinceLastBackoff} ({requestsPerSecond})", requestsSinceLastBackoff, timeSinceLastBackoff.TotalSeconds, requestsSinceLastBackoff / timeSinceLastBackoff.TotalSeconds);
						logger.LogWarning("Backing off for {backoff}ms", backoffMs);
						requestsSinceLastBackoff = 0;
						lastBackoff = now;
						break;
					default:
						logger.LogWarning("Unexpected status recieved: {statusCode}, trying again...", response.StatusCode);
						break;
				}
			}
			catch(HttpRequestException ex)
			{
				logger.LogWarning(ex, "Exception in request: {message}, trying again...", ex.Message);
			}

			await Task.Delay(backoffMs);
		}

		return null; // cancelled
	}

	public async Task<T?> requestJsonWithBackoff<T>(string path, CancellationToken stoppingToken)
	{
		HttpResponseMessage? response = await requestWithBackoff(path, stoppingToken);
		if(response is not null)
		{
			return await response.Content.ReadFromJsonAsync<T>();
		}
		return default(T);
	}

	public async Task<T?> requestEmbedded<T>(string path, string embeddedType, CancellationToken stoppingToken)
	{
		HttpResponseMessage? response = await requestWithBackoff(path, stoppingToken);
		if(response is null)
		{
			return default(T);
		}

		var json = JsonNode.Parse(await response.Content.ReadAsStreamAsync());
		if(json is null)
		{
			logger.LogWarning("Failed to parse json for {path}", path);
			return default(T);
		}

		// FIXME: handle nulls here:
		var embeddedCastObject = json["_embedded"]![embeddedType];
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        embeddedCastObject!.WriteTo(writer);
        writer.Flush();
        return JsonSerializer.Deserialize<T>(stream.ToArray());
	}
}