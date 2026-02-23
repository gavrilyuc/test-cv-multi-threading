namespace MultiThreading;

public interface IRobotsTxtService
{
	Task<bool> IsPathAllowedAsync(Uri resourceUri, CancellationToken token);
}