using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit.Abstractions;
using System.Linq;
using System;

namespace scraper.test;

public class ScraperTest
{
	private ITestOutputHelper _output;
	private Mock<ITvMazeClient> mockMazeClient;
	private List<ShowInfo> expectedPage0;
	private List<PersonInfo> dummyPersonInfo;
	private List<CastInfo> dummyCastInfo;
	private Mock<IShowCastRepository> mockShowCastRepository;

	public ScraperTest(ITestOutputHelper output)
	{
		_output = output;
		mockMazeClient = new Mock<ITvMazeClient>();

		expectedPage0 = new List<ShowInfo> {
			new ShowInfo {
				id = 1,
				name = "show-1"
			},
			new ShowInfo {
				id = 2,
				name = "show-2"
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
		mockShowCastRepository.Setup(repo => repo.lastPageCompleted()).Returns(-1);
	}

	void setupMockResponse<T>(string path, T result)
	{
		mockMazeClient.Setup(mazeClient => mazeClient.requestJsonWithBackoff<T>(path, It.IsAny<CancellationToken>()))
		.Returns(() => Task.FromResult<T?>(result));
	}

	void setupPages(int pageCount, int showsPerPage=2)
	{
		for (int pageIndex = 0; pageIndex < pageCount; ++pageIndex)
		{
			List<ShowInfo> page = new();
			for (int showIndex = 1; showIndex <= showsPerPage; ++showIndex)
			{
				var id = pageIndex * pageCount * showsPerPage + showIndex;
				page.Add(new ShowInfo { id = id, name = "show-" + id.ToString()});
				setupMockResponse($"/shows/{id}/cast", dummyCastInfo);
			}

			setupMockResponse($"/shows?page={pageIndex}", page);
		}
	}

	void setupLastProcessedPage(int page)
	{
		mockShowCastRepository = new Mock<IShowCastRepository>();
		mockShowCastRepository.Setup(repo => repo.lastPageCompleted()).Returns(page);
	}


	void setupUpsertCallback(Action<ShowInfo> callback)
	{
		mockShowCastRepository.Setup(showCastRepository => showCastRepository.upsert(It.IsAny<ShowInfo>()))
		  .Callback<ShowInfo>(callback);
	}


	[Fact]
	public async Task itLoadsPages()
	{
		setupPages(1);

		var scraper = new Scraper(_output.BuildLoggerFor<Scraper>(), mockMazeClient.Object, mockShowCastRepository.Object);

		await scraper.scrape(CancellationToken.None);

		scraper.MaxPage.Should().Be(0);
		scraper.MaxShow.Should().Be(2);
	}

	[Fact]
	public async Task itStoresShowCastDocument()
	{
		setupPages(1);

		var actualDocuments = new List<ShowInfo>();
		setupUpsertCallback((showInfo) => actualDocuments.Add(showInfo));

		var scraper = new Scraper(_output.BuildLoggerFor<Scraper>(), mockMazeClient.Object, mockShowCastRepository.Object);

		await scraper.scrape(CancellationToken.None);

		var expectedDocuments = expectedPage0.Select(showInfo => new ShowInfo { id = showInfo.id, name = showInfo.name, cast = dummyPersonInfo }).ToList();

		actualDocuments.Should().BeEquivalentTo(expectedDocuments);
	}


	[Fact]
	public async Task itStoresLastCompletedPage()
	{
		setupPages(1);

		var scraper = new Scraper(_output.BuildLoggerFor<Scraper>(), mockMazeClient.Object, mockShowCastRepository.Object);

		await scraper.scrape(CancellationToken.None);

		mockShowCastRepository.Verify(repo => repo.completePage(0), Times.Once());
	}


	[Fact]
	public async Task itStartsFromLastProcessedPage()
	{
		int pageCount = 3;
		int startPage = 1;
		setupPages(pageCount, showsPerPage: 2);
		setupLastProcessedPage(startPage);

		int pagesToComplete = pageCount - startPage - 1; // pages are 0 indexed

		var actualDocuments = new List<ShowInfo>();
		setupUpsertCallback((showInfo) => actualDocuments.Add(showInfo));

		var scraper = new Scraper(_output.BuildLoggerFor<Scraper>(), mockMazeClient.Object, mockShowCastRepository.Object);

		await scraper.scrape(CancellationToken.None);

		mockShowCastRepository.Verify(repo => repo.completePage(It.IsAny<int>()), Times.Exactly(pagesToComplete));
	}

	// itSkipsIfCastListNotFound
	// itWarnsIfCastNotPerson
}