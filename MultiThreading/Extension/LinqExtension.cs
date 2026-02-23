namespace MultiThreading;

internal static class LinqExtension
{
	public static IEnumerable<string> WhereNotNullOrWhitespace(this IEnumerable<string?> source)
	{
		return source.Where(s => !string.IsNullOrWhiteSpace(s))!.Select(s => s!.Trim());
	}
}