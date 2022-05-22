using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Collections.Generic;
using FluentAssertions;

namespace api.test;


class ScraperApiApplication : WebApplicationFactory<Program>
{
	private IShowCastReadRepository repo;

	public ScraperApiApplication(IShowCastReadRepository repo)
    {
		this.repo = repo;
	}

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(IShowCastReadRepository));
			services.TryAddSingleton<IShowCastReadRepository>(sp => repo);
		});

        return base.CreateHost(builder);
    }
}


public class ApiTest
{
    private Task<List<object>> generatePage(int page, int pageSize)
    {
		List<object> result = new();
		for (int i = 0; i < pageSize; ++i)
		{
			int id = pageSize * page + i;
			result.Add(new { id = id, name = "show " + id.ToString() });
		}

        return Task.FromResult(result);
    }

    [Fact]
    public async Task itReturnsDocumentsFromRepository()
    {
		var mockRepo = new Mock<IShowCastReadRepository>();
		mockRepo.Setup(repo => repo.getShowCasts(It.IsAny<int>(), It.IsAny<int>()))
			.Returns((int page, int pageSize) => generatePage(page, pageSize) );

		await using var application = new ScraperApiApplication(mockRepo.Object);

        var client = application.CreateClient();
        var shows = await client.GetFromJsonAsync<List<object>>("/shows?page=1&size=1");

		shows.Should().HaveCount(1);
	}
}