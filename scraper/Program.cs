using System.Reflection;
using Serilog;
using scraper;

var builder = WebApplication.CreateBuilder(args);
setupLogging(builder);

builder.Services.AddSingleton<IShowCastRepository, ShowCastRepository>();
builder.Services.AddSingleton<ITvMazeClient, TvMazeClient>();
builder.Services.AddSingleton<Scraper>();
builder.Services.AddHostedService<ScraperService>();

var app = builder.Build();

app.MapGet("/", () => "Scraper");

app.Run();


static void setupLogging(WebApplicationBuilder builder) {
	builder.Logging.ClearProviders();
	var entry = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;
	var loggerConfiguration = new LoggerConfiguration()
		.Enrich.FromLogContext()
		.Enrich.WithProperty("Application", entry)
		.WriteTo.Console()
		;
	if (Environment.GetEnvironmentVariable("SEQ_SERVICE_PROTOCOL") != null)
	{
		var seq_conection_string = Environment.GetEnvironmentVariable("SEQ_SERVICE_PROTOCOL") + "://" +
								Environment.GetEnvironmentVariable("SEQ_SERVICE_HOST") + ":" +
								Environment.GetEnvironmentVariable("SEQ_SERVICE_PORT");

		loggerConfiguration.WriteTo.Seq(seq_conection_string);
	}

	Log.Logger = loggerConfiguration.CreateLogger();

	builder.Logging.AddSerilog(Log.Logger);
	Serilog.Debugging.SelfLog.Enable(Console.Error);
}