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
	private Mock<IShowCastRepository> mockShowCastRepository;

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

		mockShowCastRepository = new Mock<IShowCastRepository>();
	}

  private void setupMockResponse<T>(string path, T result)
  {
		mockMazeClient.Setup(mazeClient => mazeClient.requestJsonWithBackoff<T>(path, It.IsAny<CancellationToken>()))
        .Returns(() => Task.FromResult<T?>(result));
  }


  [Fact]
  public async Task itLoadsPages()
  {
		setupMockResponse("/shows?page=0", dummyShowInfo);
		setupMockResponse("/shows/1/cast", dummyCastInfo);
		setupMockResponse("/shows/2/cast", dummyCastInfo);

		var scraper = new Scraper(_output.BuildLoggerFor<Scraper>(), mockMazeClient.Object, mockShowCastRepository.Object);

		await scraper.scrape(CancellationToken.None);

		scraper.MaxPage.Should().Be(0);
		scraper.MaxShow.Should().Be(2);
	}

  [Fact]
  public async Task itStoresShowCastDocument()
  {
		setupMockResponse("/shows?page=0", dummyShowInfo);
		setupMockResponse("/shows/1/cast", dummyCastInfo);
		setupMockResponse("/shows/2/cast", dummyCastInfo);

		var actualDocuments = new List<ShowInfo>();
		mockShowCastRepository.Setup(showCastRepository => showCastRepository.upsert(It.IsAny<ShowInfo>()))
		  .Callback<ShowInfo>((showInfo) => actualDocuments.Add(showInfo));
		dummyShowInfo[0].cast.Should().BeEmpty();

		var scraper = new Scraper(_output.BuildLoggerFor<Scraper>(), mockMazeClient.Object, mockShowCastRepository.Object);

		await scraper.scrape(CancellationToken.None);

		var expectedDocuments = dummyShowInfo.Select(showInfo => new ShowInfo { id = showInfo.id, name = showInfo.name, cast = dummyPersonInfo }).ToList();

		actualDocuments.Should().BeEquivalentTo(expectedDocuments);

		//mockShowCastRepository.Verify(showCastRepository => showCastRepository.upsert(expectedDocument[1]), Times.Once());
	}

  // itSkipsIfCastListNotFound
  // itWarnsIfCastNotPerson
}