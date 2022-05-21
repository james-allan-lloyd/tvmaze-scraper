using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit.Abstractions;
using System.Linq;

namespace scraper.test;

public class ScraperTest
{
	private ITestOutputHelper _output;
	private Mock<ITvMazeClient> mockMazeClient;
	private List<ShowInfo> dummyShowInfo;
	private List<PersonInfo> dummyPersonInfo;
	private List<CastInfo> dummyCastInfo;

	public ScraperTest(ITestOutputHelper output)
	{
		_output = output;
		mockMazeClient = new Mock<ITvMazeClient>();

		dummyShowInfo = new List<ShowInfo> {
      new ShowInfo {
        id = 1,
        name = "show1"
      },
      new ShowInfo {
        id = 2,
        name = "show2"
      }
    };

		dummyPersonInfo = new List<PersonInfo> {
      new PersonInfo  {
        id = 1,
        name = "person1",
        birthday = "2000-1-1",
      },
      new PersonInfo  {
        id = 2,
        name = "person2",
        birthday = "2000-1-2",
      },
    };


		dummyCastInfo = dummyPersonInfo.Select(personInfo => new CastInfo { person = personInfo }).ToList();
	}

  private void setupMockResponse<T>(string path, T result)
  {
		mockMazeClient.Setup(mazeClient => mazeClient.requestJsonWithBackoff<T>(path, It.IsAny<CancellationToken>()))
        .Returns(() => Task.FromResult<T?>(result));
  }


  [Fact]
  public async Task itLoadsPagesAsync()
  {
		setupMockResponse("/shows?page=0", dummyShowInfo);
		setupMockResponse("/shows/1/cast", dummyCastInfo);
		setupMockResponse("/shows/2/cast", dummyCastInfo);

		var scraper = new Scraper(_output.BuildLoggerFor<Scraper>(), mockMazeClient.Object);

		await scraper.scrape(CancellationToken.None);

		scraper.MaxPage.Should().Be(0);
		scraper.MaxShow.Should().Be(2);
	}

  // itSkipsIfCastListNotFound
  // itWarnsIfCastNotPerson
}