namespace MultiThreading;

internal sealed class RobotsTxtParser : IRobotsTxtParser
{
	public RobotsTxt AllowAll() => new([], []);

	public RobotsTxt Parse(string content, string ourUserAgent)
	{
		var rules = AllowAll();
		var lines = content.Split(RobotsTxtConstants.NewLineDelimiters, StringSplitOptions.RemoveEmptyEntries)
			.Select(l => l.Trim())
			.Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith(RobotsTxtConstants.CommentSymbol))
			.ToList();

		var blocks = new List<List<string>>();
		List<string>? current = null;
		foreach (var line in lines)
		{
			if (line.StartsWith(RobotsTxtConstants.UserAgentPropertyName, StringComparison.OrdinalIgnoreCase))
			{
				current = [line];

				blocks.Add(current);
			}
			else
			{
				current?.Add(line);
			}
		}

		var selectedBlocks = blocks.Where(b =>
		{
			var uas = b.Where(x => x.StartsWith(RobotsTxtConstants.UserAgentPropertyName, StringComparison.InvariantCultureIgnoreCase))
				.Select(x => x[(x.IndexOf(RobotsTxtConstants.PairDelimiter) + 1)..].Trim().ToLowerInvariant());

			return uas.Any(u => u == RobotsTxtConstants.Any
				|| ourUserAgent.Contains(u, StringComparison.InvariantCultureIgnoreCase)
				|| u.Contains(ourUserAgent, StringComparison.InvariantCultureIgnoreCase));
		}).ToList();

		if (selectedBlocks.Count == 0)
		{
			selectedBlocks = blocks.Where(b =>
			{
				var uas = b.Where(x => x.StartsWith(RobotsTxtConstants.UserAgentPropertyName, StringComparison.OrdinalIgnoreCase))
					.Select(x => x[(x.IndexOf(RobotsTxtConstants.PairDelimiter) + 1)..].Trim().ToLowerInvariant());

				return uas.Any(u => u == RobotsTxtConstants.Any);
			}).ToList();
		}

		if (selectedBlocks.Count == 0)
		{
			return rules;
		}

		foreach (var b in selectedBlocks)
		{
			foreach (var l in b)
			{
				var idx = l.IndexOf(RobotsTxtConstants.PairDelimiter);
				if (idx <= 0)
				{
					continue;
				}

				var key = l[..idx].Trim();
				var val = l[(idx + 1)..].Trim();

				if (key.Equals(RobotsTxtConstants.DisallowPropertyName, StringComparison.OrdinalIgnoreCase))
				{
					if (!string.IsNullOrEmpty(val))
					{
						rules.Disallow.Add(val);
					}
				}
				else if (key.Equals(RobotsTxtConstants.AllowPropertyName, StringComparison.OrdinalIgnoreCase))
				{
					if (!string.IsNullOrEmpty(val))
					{
						rules.Allow.Add(val);
					}
				}
			}
		}

		return rules;
	}
}