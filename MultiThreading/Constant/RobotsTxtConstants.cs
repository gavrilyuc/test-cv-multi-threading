namespace MultiThreading;

public static class RobotsTxtConstants
{
	public const string RobotsTxtFileName = "robots.txt";

	public static readonly string WebUrlDelimiter = Constants.WebUrlDelimiterChar.ToString();
	public static readonly char[] NewLineDelimiters = ['\n', '\r'];

	public const string UserAgentPropertyName = "User-Agent";
	public const string DisallowPropertyName = "Disallow";
	public const string AllowPropertyName = "Allow";

	public const char CommentSymbol = '#';
	public const char PairDelimiter = ':';
	public const string Any = "*";
}