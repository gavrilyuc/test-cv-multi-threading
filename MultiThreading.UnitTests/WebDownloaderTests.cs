using System.Net;
using System.Net.Mime;
using Microsoft.Extensions.DependencyInjection;

namespace MultiThreading.UnitTests;

public sealed class WebDownloaderTests : MultiThreadingUnitTest
{
	private readonly string _tempDir;

	public WebDownloaderTests()
	{
		_tempDir = Path.Combine(Path.GetTempPath(), "mt_int_" + Guid.NewGuid());

		Directory.CreateDirectory(_tempDir);

		var cfg = Services.GetRequiredService<ApplicationConfiguration>();

		cfg.OutputDir = _tempDir;
	}

	public override void Dispose()
	{
		base.Dispose();

		if (Directory.Exists(_tempDir))
		{
			Directory.Delete(_tempDir, true);
		}
	}

	[Fact]
	public async Task StartAsync_Downloads_Page_And_Asset()
	{
		var downloader = Services.GetRequiredService<WebDownloader>();

		// Act
		await downloader.StartAsync(new Uri("http://example.com/"), CancellationToken.None);

		// Assert
		var files = Directory.GetFiles(_tempDir, "*.*", SearchOption.AllDirectories);

		Assert.Contains(files, f => f.EndsWith("index.html", StringComparison.Ordinal));
		Assert.Contains(files, f => f.EndsWith("style.css", StringComparison.Ordinal));
	}

	[Theory]
	[InlineData("example.com", "example.com", DownloadMode.Domain, true)]
	[InlineData("sub.example.com", "example.com", DownloadMode.Domain, false)]
	[InlineData("sub.example.com", "example.com", DownloadMode.SubDomains, true)]
	[InlineData("example.com", "example.sub.com", DownloadMode.SubDomains, true)]
	[InlineData("example.com", "example2.com", DownloadMode.Any, true)]
	public void UriHostMatches(string candidateHost, string rootHost, DownloadMode mode, bool assert)
	{
		var result = WebDownloader.UriHostMatches(candidateHost, rootHost, mode);
		if (assert)
		{
			Assert.True(result);
		}
		else
		{
			Assert.False(result);
		}
	}

	[Fact]
	public void TryResolveUrl()
	{
		var baseUri = new Uri("https://example.com/path/page.html");

		Assert.True(WebDownloader.TryResolveHttpUrl(baseUri, "//cdn.example.com/lib.js", out var r1));
		Assert.Equal("https://cdn.example.com/lib.js", r1.ToString());

		Assert.True(WebDownloader.TryResolveHttpUrl(baseUri, "/images/photo.png", out var r2));
		Assert.Equal("https://example.com/images/photo.png", r2.ToString());

		Assert.True(WebDownloader.TryResolveHttpUrl(baseUri, "other/page2.html", out var r3));
		Assert.Equal("https://example.com/path/other/page2.html", r3.ToString());
	}

	[Fact]
	public void MapUriToFilePath_Index()
	{
		// Arrange
		var downloader = Services.GetRequiredService<WebDownloader>();

		var uri = new Uri("http://example.com/about/");
		var path = downloader.MapUriToFilePath(uri, true, MediaTypeNames.Text.Html);

		Assert.EndsWith("index.html", path.Replace("\\", "/"));
		Assert.Contains("example.com", path.Replace("\\", "/"));
	}

	[Fact]
	public void MapUriToFilePath_FlattenedFileName()
	{
		// Arrange
		var downloader = Services.GetRequiredService<WebDownloader>();

		var uri = new Uri("http://example.com/about/team/");
		var path = downloader.MapUriToFilePath(uri, false, MediaTypeNames.Text.Html);

		// flattened: filename like about_team.html (or with numeric suffix if exists)
		var p = path.Replace("\\", "/");
		Assert.Contains("example.com", p);
		Assert.Matches(@"about_team(\.html|(_\d+)?\.html)$", p);
	}

	protected override IHttpClientFactory BuildFakeClientFactory()
	{
		return new HttpClientFactory();
	}

	// here we can use some fakes libraries (for example AutoFixture, Bogus or something else)
	// but in current TEST realization we can exclude it 
	private sealed class HttpClientFactory : IHttpClientFactory
	{
		public HttpClient CreateClient(string name)
		{
			var html = """
							<html>
								<head>
									<link rel="stylesheet" href="/style.css" />
								</head>
								<body>Hello</body>
							</html>
						""";

			var handler = new FakeHandler(req =>
			{
				if (req.RequestUri!.AbsolutePath == "/")
				{
					return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(html) };
				}

				if (req.RequestUri.AbsolutePath == "/style.css")
				{
					return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("body { background: red; }") };
				}

				if (req.RequestUri.AbsolutePath == "/robots.txt")
				{
					return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("User-agent: *\nDisallow:") };
				}

				return new HttpResponseMessage(HttpStatusCode.NotFound);
			});

			return new HttpClient(handler);
		}

		private sealed class FakeHandler : HttpMessageHandler
		{
			private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

			public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
			{
				_handler = handler;
			}

			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
				CancellationToken cancellationToken)
			{
				return Task.FromResult(_handler(request));
			}
		}
	}
}