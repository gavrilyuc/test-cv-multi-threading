namespace MultiThreading;

public interface IRobotsTxtParser
{
	RobotsTxt AllowAll();

	RobotsTxt Parse(string content, string ourUserAgent);
}