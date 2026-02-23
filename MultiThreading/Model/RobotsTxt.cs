namespace MultiThreading;

public sealed class RobotsTxt(List<string> allow, List<string> disallow)
{
	public List<string> Disallow { get; } = disallow;

	public List<string> Allow { get; } = allow;

	public bool IsAllowed(string path)
	{
		path = path.Trim();

		var matchedAllow = GetValue(Allow, path);
		if (matchedAllow != null)
		{
			return true;
		}

		var matchedDis = GetValue(Disallow, path);

		return matchedDis == null;
	}

	private static string? GetValue(IEnumerable<string> list, string path)
	{
		return list.Where(a => !string.IsNullOrEmpty(a))
			.OrderByDescending(a => a.Length)
			.FirstOrDefault(a => path.StartsWith(a, StringComparison.OrdinalIgnoreCase));
	}
}