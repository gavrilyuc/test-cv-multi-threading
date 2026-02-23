using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Logging;

namespace MultiThreading;

internal sealed class WebDownloader(
	IHttpClientFactory httpFactory,
	IRobotsTxtService robots,
	IHtmlParser parser,
	ILogger<WebDownloader> logger,
	ApplicationConfiguration options)
	: IWebDownloader
{
	private readonly ConcurrentDictionary<string, bool> _visited = new(StringComparer.InvariantCultureIgnoreCase);
	private int _activeWorkCount = 0;

	public async Task StartAsync(Uri root, CancellationToken token)
	{
		logger.LogInformation("Start scanning of {root} (maxDepth={depth}, maxConcurrency={concurrency})",
			root, options.MaxDepth, options.MaxConcurrency);

		var channel = Channel.CreateBounded<(Uri uri, int depth)>(new BoundedChannelOptions(options.MaxLinks)
		{
			FullMode = BoundedChannelFullMode.Wait,
			SingleReader = false,
			SingleWriter = false
		});

		await AddUrlToChannelAsync(channel.Writer, root, 0, token).ConfigureAwait(false);

		var workers = new Task[options.MaxConcurrency];
		for (int i = 0; i < options.MaxConcurrency; i++)
		{
			workers[i] = RunWorkerAsync(channel, root.Host, token);
		}

		await Task.WhenAll(workers).ConfigureAwait(false);

		logger.LogInformation("Scan finished. All workers stopped.");
	}

	#region Private Methods

	private async Task RunWorkerAsync(Channel<(Uri uri, int depth)> channel, string rootHost, CancellationToken token)
	{
		try
		{
			await foreach (var (uri, depth) in channel.Reader.ReadAllAsync(token).ConfigureAwait(false))
			{
				try
				{
					await ProcessUrlAsync(uri, depth, rootHost, channel.Writer, token).ConfigureAwait(false);
				}
				finally
				{
					var remaining = Interlocked.Decrement(ref _activeWorkCount);
					if (remaining == 0)
					{
						channel.Writer.TryComplete();
					}
				}
			}
		}
		catch (OperationCanceledException)
		{
			// ignored
		}
	}

	private async Task ProcessUrlAsync(Uri uri, int depth, string rootHost, ChannelWriter<(Uri, int)> writer, CancellationToken token)
	{
		if (!UriHostMatches(uri.Host, rootHost, options.DownloadMode))
		{
			return;
		}

		var allowed = await robots.IsPathAllowedAsync(uri, token).ConfigureAwait(false);
		if (!allowed)
		{
			logger.LogInformation("Denied by {fileName}: {uri}", RobotsTxtConstants.RobotsTxtFileName, uri);

			return;
		}

		try
		{
			logger.LogInformation("[Depth {depth}] Start Downloading: {uri}", depth, uri);

			var client = httpFactory.CreateClient(Constants.HttpClientName);

			using var resp = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);

			if (!resp.IsSuccessStatusCode)
			{
				return;
			}

			var contentType = resp.Content.Headers.ContentType?.MediaType ?? string.Empty;

			if (contentType.Contains(Constants.HtmlContentType)
				|| Constants.HtmlFileExtensions.Any(e => uri.AbsolutePath.EndsWith(e, StringComparison.Ordinal))
				|| uri.AbsolutePath == string.Empty
				|| uri.AbsolutePath == RobotsTxtConstants.WebUrlDelimiter)
			{
				var html = await resp.Content.ReadAsStringAsync(token).ConfigureAwait(false);

				await SaveTextContentAsync(uri, html, contentType, token).ConfigureAwait(false);

				if (depth < options.MaxDepth)
				{
					await ExtractAndEnqueueUrlsAsync(writer, uri, rootHost, depth + 1, html, token).ConfigureAwait(false);
				}
			}
			else
			{
				var bytes = await resp.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
				await SaveBinaryContentAsync(uri, bytes, contentType, token).ConfigureAwait(false);
			}
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			logger.LogWarning("Error processing {uri}: {msg}", uri, ex.Message);
		}
	}

	private async Task AddUrlToChannelAsync(ChannelWriter<(Uri, int)> writer, Uri uri, int depth, CancellationToken token)
	{
		var key = NormalizeKey(uri);
		if (_visited.TryAdd(key, true))
		{
			Interlocked.Increment(ref _activeWorkCount);
			await writer.WriteAsync((uri, depth), token).ConfigureAwait(false);
		}
	}

	private async Task ExtractAndEnqueueUrlsAsync(
		ChannelWriter<(Uri, int)> writer,
		Uri baseUri,
		string rootHost,
		int depth,
		string html,
		CancellationToken token)
	{
		var doc = await parser.ParseDocumentAsync(html).ConfigureAwait(false);

		var references = doc.QuerySelectorAll(Constants.HtmlImgTag)
			.Select(e => e.GetAttribute(Constants.HtmlSrcAttribute))
			.Concat(doc.QuerySelectorAll(Constants.HtmlScriptTag).Select(e => e.GetAttribute(Constants.HtmlSrcAttribute)))
			.Concat(doc.QuerySelectorAll(Constants.HtmlStyleSelector).Select(e => e.GetAttribute(Constants.HtmlHrefAttribute)))
			.Concat(doc.QuerySelectorAll(Constants.HtmlATag).Select(e => e.GetAttribute(Constants.HtmlHrefAttribute)))
			.WhereNotNullOrWhitespace()
			.Distinct();

		foreach (var raw in references)
		{
			if (TryResolveHttpUrl(baseUri, raw, out var resolved)
				&& (resolved.Scheme == Uri.UriSchemeHttp || resolved.Scheme == Uri.UriSchemeHttps)
				&& UriHostMatches(resolved.Host, rootHost, options.DownloadMode))
			{
				await AddUrlToChannelAsync(writer, resolved, depth, token).ConfigureAwait(false);
			}
		}
	}

	private static string NormalizeKey(Uri u)
	{
		return u.GetLeftPart(UriPartial.Path)
			.TrimEnd(Constants.WebUrlDelimiterChar)
			.ToLowerInvariant();
	}

	internal string MapUriToFilePath(Uri uri, bool makeDirectoryForIndex, string defaultExtensionFromMediaType)
	{
		var hostDir = Path.Combine(options.OutputDir, SanitizeFileName(uri.Host));
		var segments = uri.AbsolutePath.Split(RobotsTxtConstants.WebUrlDelimiter, StringSplitOptions.RemoveEmptyEntries)
			.Select(s => SanitizeFileName(Uri.UnescapeDataString(s))).ToArray();

		string filePath;

		var isDirectoryLike = uri.AbsolutePath.EndsWith(RobotsTxtConstants.WebUrlDelimiter, StringComparison.Ordinal)
			|| string.IsNullOrEmpty(Path.GetFileName(uri.AbsolutePath));
		if (isDirectoryLike)
		{
			if (makeDirectoryForIndex)
			{
				var dir = Path.Combine(hostDir, Path.Combine(segments));

				Directory.CreateDirectory(dir);

				filePath = Path.Combine(dir, Constants.RootFileName);
			}
			else
			{
				Directory.CreateDirectory(hostDir);

				if (segments.Length == 0)
				{
					filePath = Path.Combine(hostDir, Constants.RootFileName);
				}
				else
				{
					var fileName = string.Join(Constants.FileSnakeDelimiter, segments) + Constants.HtmlFileExtensions[0];

					filePath = Path.Combine(hostDir, fileName);
				}
			}
		}
		else
		{
			filePath = Path.Combine(hostDir, Path.Combine(segments));
		}

		var ext = Path.GetExtension(filePath);
		if (string.IsNullOrEmpty(ext))
		{
			var extFromMedia = ExtensionFromMediaType(defaultExtensionFromMediaType);
			if (!string.IsNullOrEmpty(extFromMedia))
			{
				filePath += extFromMedia;
			}
			else
			{
				filePath += Constants.UnknownFileExtension;
			}
		}

		var finalPath = filePath;
		int i = 1;

		while (File.Exists(finalPath))
		{
			var baseName = Path.GetFileNameWithoutExtension(filePath);
			var dir = Path.GetDirectoryName(filePath)!;
			var extension = Path.GetExtension(filePath);

			finalPath = Path.Combine(dir, $"{baseName}_{i++}{extension}");
		}

		return finalPath;
	}

	private async Task SaveTextContentAsync(Uri uri, string text, string contentType, CancellationToken token)
	{
		var path = MapUriToFilePath(uri, true, contentType);

		Directory.CreateDirectory(Path.GetDirectoryName(path)!);

		await File.WriteAllTextAsync(path, text, Encoding.UTF8, token);

		logger.LogDebug("Saved HTML -> {path}", path);
	}

	private async Task SaveBinaryContentAsync(Uri uri, byte[] data, string mediaType, CancellationToken token)
	{
		var path = MapUriToFilePath(uri, false, mediaType);

		Directory.CreateDirectory(Path.GetDirectoryName(path)!);

		await File.WriteAllBytesAsync(path, data, token);

		logger.LogDebug("Saved file -> {path} ({len} bytes)", path, data.Length);
	}

	#endregion

	#region Static Methods

	internal static bool UriHostMatches(string candidateHost, string rootHost, DownloadMode mode)
	{
		if (string.Equals(candidateHost, rootHost, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (mode != DownloadMode.SubDomains)
		{
			return mode == DownloadMode.Any;
		}

		var rootNames = rootHost.Split(Constants.Dot, StringSplitOptions.RemoveEmptyEntries);
		var candidateNames = candidateHost.Split(Constants.Dot, StringSplitOptions.RemoveEmptyEntries);

		if (candidateHost.EndsWith(Constants.Dot + rootHost, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (rootNames.First() == candidateNames.First() && rootNames.Last() == candidateNames.Last())
		{
			return true;
		}

		return false;
	}

	internal static bool TryResolveHttpUrl(Uri baseUri, string raw, out Uri resolved)
	{
		resolved = null!;

		if (string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}

		try
		{
			if (raw.StartsWith(Constants.HttpQueryStart, StringComparison.Ordinal))
			{
				resolved = new Uri(baseUri.Scheme + Constants.UriSchemaDelimiter + raw);

				return true;
			}

			if (Uri.TryCreate(raw, UriKind.Absolute, out var abs))
			{
				if (string.Equals(abs.Scheme, baseUri.Scheme, StringComparison.OrdinalIgnoreCase))
				{
					resolved = abs;

					return true;
				}
			}

			if (Uri.TryCreate(baseUri, raw, out var rel))
			{
				if (string.Equals(rel.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
					|| string.Equals(rel.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
				{
					resolved = rel;

					return true;
				}
			}
		}
		catch
		{
			// ignored
		}

		return false;
	}

	private static string ExtensionFromMediaType(string? mediaType)
	{
		if (string.IsNullOrWhiteSpace(mediaType))
		{
			return string.Empty;
		}

		mediaType = mediaType.ToLowerInvariant();

		if (Constants.MediaTypeToFileExtension.TryGetValue(mediaType, out var result))
		{
			return result;
		}

		return string.Empty;
	}

	private static string SanitizeFileName(string s)
	{
		s = Path.GetInvalidFileNameChars()
			.Aggregate(s, (current, c) => current.Replace(c, Constants.FileSnakeDelimiter));

		foreach (var (key, value) in Constants.UnsupportedWebFileNameSymbols)
		{
			s = s.Replace(key, value);
		}

		return s;
	}

	#endregion
}