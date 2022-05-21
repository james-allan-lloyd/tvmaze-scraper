using System.Reflection;
using Serilog;
using api;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
setupLogging(builder);
builder.Services.AddSingleton<ShowCastReadRepository>();

var app = builder.Build();

app.MapGet("/", () => "API");

app.MapGet("/shows/", async ([FromServices] ShowCastReadRepository showCastRepository) => {
    return Results.Ok(await showCastRepository.getShowCasts());
});

app.Run();


static void setupLogging(WebApplicationBuilder builder) {
	var seq_conection_string = Environment.GetEnvironmentVariable("SEQ_SERVICE_PROTOCOL") + "://" +
							Environment.GetEnvironmentVariable("SEQ_SERVICE_HOST") + ":" +
							Environment.GetEnvironmentVariable("SEQ_SERVICE_PORT");

	builder.Logging.ClearProviders();
	var entry = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;
	Log.Logger = new LoggerConfiguration()
				.Enrich.FromLogContext()
				.Enrich.WithProperty("Application", entry)
				.WriteTo.Console()
				.WriteTo.Seq(seq_conection_string)
				.CreateLogger();

	builder.Logging.AddSerilog(Log.Logger);
	Serilog.Debugging.SelfLog.Enable(Console.Error);
	Log.Information("Logging to " + seq_conection_string);
}