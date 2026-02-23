using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MultiThreading;

public sealed class RobotsTxtService(
	IHttpClientFactory httpFactory,
	IRobotsTxtParser parser,
	ILogger<RobotsTxtService> logger,
	RobotsConfiguration robotsOptions)
	: IRobotsTxtService
{
	private readonly string _ourUserAgent = robotsOptions.UserAgent;

	private readonly ConcurrentDictionary<string, RobotsTxt> _cache = new(StringComparer.InvariantCultureIgnoreCase);

	public async Task<bool> IsPathAllowedAsync(Uri resourceUri, CancellationToken token)
	{
		var host = resourceUri.Host;
		var rules = await GetRulesForHostAsync(host, resourceUri.Scheme, token).ConfigureAwait(false);
		if (rules == null)
		{
			return true;
		}

		var path = string.IsNullOrEmpty(resourceUri.PathAndQuery) ? RobotsTxtConstants.WebUrlDelimiter : resourceUri.PathAndQuery;

		return rules.IsAllowed(path);
	}

	private async Task<RobotsTxt?> GetRulesForHostAsync(string host, string scheme, CancellationToken token)
	{
		if (_cache.TryGetValue(host, out var rules))
		{
			return rules;
		}

		var robotsUri = GetRobotsTxtUri(scheme, host);

		try
		{
			var client = httpFactory.CreateClient(Constants.HttpClientName);

			using var resp = await client.GetAsync(robotsUri, token).ConfigureAwait(false);
			if (!resp.IsSuccessStatusCode)
			{

				logger.LogInformation("{fileName} not found for {robotsUri}: {status}",
					RobotsTxtConstants.RobotsTxtFileName, robotsUri, resp.StatusCode);

				rules = parser.AllowAll();

				_cache[host] = rules;

				return rules;
			}

			var content = await resp.Content.ReadAsStringAsync(token).ConfigureAwait(false);

			rules = parser.Parse(content, _ourUserAgent);

			_cache[host] = rules;

			logger.LogInformation("Parsed {fileName} for {host}: Disallow {disallow}, Allow {allow}",
				RobotsTxtConstants.RobotsTxtFileName, host, rules.Disallow.Count, rules.Allow.Count);

			return rules;
		}
		catch (Exception ex)
		{
			logger.LogDebug(ex, "Failed to fetch {fileName} for {host}", RobotsTxtConstants.RobotsTxtFileName, host);

			rules = parser.AllowAll();

			_cache[host] = rules;

			return rules;
		}
	}

	private static Uri GetRobotsTxtUri(string scheme, string host)
	{
		return new UriBuilder(scheme, host)
		{
			Path = RobotsTxtConstants.WebUrlDelimiter + RobotsTxtConstants.RobotsTxtFileName
		}.Uri;
	}
}