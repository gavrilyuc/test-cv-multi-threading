namespace MultiThreading;

public interface IWebDownloader
{
	Task StartAsync(Uri uri, CancellationToken token);
}