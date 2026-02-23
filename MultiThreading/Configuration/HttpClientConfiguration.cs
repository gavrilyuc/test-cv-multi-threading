namespace MultiThreading;

public sealed record HttpClientConfiguration
{
	public int TimeoutSeconds { get; init; } = 30;

	public string UserAgent { get; init; } = "MultiThreadingBot/1.0";
}