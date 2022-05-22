using System.Reflection;
using Serilog;
using api;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
setupLogging(builder);
builder.Services.AddSingleton<IShowCastReadRepository, ShowCastReadRepository>();

var app = builder.Build();

app.MapGet("/", () => "API");

app.MapGet("/shows", async ([FromServices] IShowCastReadRepository showCastRepository, int? page, int? size) => {
	if(page <= 0)
	{
		return Results.BadRequest(new {error = "Invalid page: should be greater than zero"});
	}
	if(size <= 0)
	{
		return Results.BadRequest(new {error = "Invalid size: should be greater than zero"});
	}
    return Results.Ok(await showCastRepository.getShowCasts(page ?? 1, size ?? 10));
});

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