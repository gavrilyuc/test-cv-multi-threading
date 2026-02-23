using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MultiThreading;

internal static class Program
{
	private static void PrintHelper(ILogger<WebDownloader> logger)
	{
		logger.LogInformation("Usage: dotnet run -- <rootUrl> [maxDepth] [maxConcurrency] [downloadMode] [maxAllowedLinks]");
		logger.LogInformation("<rootUrl> - separated by comma urls which you need to download (works recursive)");
		logger.LogInformation("[maxDepth] - maximum allowed depth searching relations");
		logger.LogInformation("[maxConcurrency] - maximum allowed concurrency async threads for downloading");
		logger.LogInformation("[downloadMode] - any - download all links, subDomain - download all links by root domain"
			+ " and subdomains, domain - download only from root domain");
		logger.LogInformation("[maxAllowedLinks] - maximum allowed links on the same time in queue for next downloading");
		logger.LogInformation("Example: dotnet run google.com,github.com 20 15");
	}

	private static async Task<int> StartApplication(
		IServiceProvider services,
		ILogger<WebDownloader> logger,
		string rootUrl,
		CancellationToken token)
	{
		var cfg = services.GetRequiredService<ApplicationConfiguration>();

		Directory.CreateDirectory(cfg.OutputDir);

		var downloader = services.GetRequiredService<IWebDownloader>();

		var links = rootUrl.Split(Constants.CommaDelimiter, StringSplitOptions.RemoveEmptyEntries);
		if (links.Length == 0)
		{
			PrintHelper(logger);

			return 1;
		}

		var processed = 0;
		foreach (var url in links)
		{
			var urlItem = url;
			foreach (var delimiter in Constants.QuotesDelimiter)
			{
				urlItem = urlItem.Replace(delimiter, string.Empty);
			}

			urlItem = urlItem.Trim();

			Uri? webPageUri = null;
			if (!urlItem.Contains(Constants.UriSchemaDelimiter + Constants.HttpQueryStart, StringComparison.Ordinal))
			{
				urlItem = Constants.HttpsWithSchemaDelimiter + urlItem;
			}

			if (Uri.TryCreate(urlItem, UriKind.Absolute, out var tmp)
				&& (tmp.Scheme == Uri.UriSchemeHttp || tmp.Scheme == Uri.UriSchemeHttps))
			{
				webPageUri = tmp;
			}
			else
			{
				logger.LogWarning("An element inside rootUrl parameter is invalid, skip invalid url for {url}", urlItem);
			}

			if (webPageUri == null)
			{
				continue;
			}

			await downloader.StartAsync(webPageUri, token);

			processed++;
		}

		if (processed == 0)
		{
			logger.LogInformation("Cannot find any URLs for downloading, please set correct rootUrl parameter");

			PrintHelper(logger);

			return 1;
		}

		logger.LogInformation("The download was completed successfully");

		return 0;
	}

	private static async Task<int> Main(string[] args)
	{
		using var host = args.BuildConsoleApplicationHost();

		using var scope = host.Services.CreateScope();
		var services = scope.ServiceProvider;

		var cts = new CancellationTokenSource();

		var logger = services.GetRequiredService<ILogger<WebDownloader>>();

		if (args.Length < 1)
		{
			PrintHelper(logger);

			return 1;
		}

		Console.CancelKeyPress += (s, e) =>
		{
			logger.LogInformation("Cancellation requested (cancel key pressed)");

			e.Cancel = true;

			cts.Cancel();
		};

		try
		{
			var result = await StartApplication(services, logger, args[0], cts.Token);

			return result;
		}
		catch (OperationCanceledException)
		{
			logger.LogInformation("Canceled by user");

			return 2;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "An error occurred while loading web pages");

			return 3;
		}
	}
}